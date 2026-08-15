using System.Globalization;
using Cosmora.Context;
using Cosmora.Models.ViewModels;
using Cosmora.Services.ML;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

namespace Cosmora.Services;

public class BinaryClassificationService : IBinaryClassificationService
{
    private readonly CosmoraDbContext _db;
    private const int Threshold = 7000;   // case'in verdiği eşik
    private const double TestFraction = 0.2;

    public BinaryClassificationService(CosmoraDbContext db) => _db = db;

    public async Task<BinaryViewModel> RunAsync()
    {
        var vm = new BinaryViewModel { Threshold = Threshold };

        // 1) SQL'de (şehir, yıl, ay) bazında toplam adet — 1M değil, ~1110 satır gelir
        var monthly = await _db.Sales
            .GroupBy(s => new { s.CityId, s.OrderDate.Year, s.OrderDate.Month })
            .Select(g => new
            {
                g.Key.CityId,
                g.Key.Year,
                g.Key.Month,
                Total = g.Sum(x => (long)x.Quantity)
            })
            .ToListAsync();

        if (monthly.Count == 0) return vm;

        // Şehir adları (küçük tablo)
        var cityNames = await _db.Cities.ToDictionaryAsync(c => c.Id, c => c.Name);

        // 2) İlk ve son ay YARIM (15.08.2023 ve 15.08.2026) — onları çıkar
        int minKey = monthly.Min(x => x.Year * 100 + x.Month);
        int maxKey = monthly.Max(x => x.Year * 100 + x.Month);
        var full = monthly
            .Where(x => (x.Year * 100 + x.Month) != minKey
                     && (x.Year * 100 + x.Month) != maxKey)
            .ToList();

        // 3) Sliding window ile eğitim örnekleri üret (şehir bazında sırala)
        var samples = new List<MonthlyCitySample>();
        var liveInputs = new List<(int CityId, MonthlyCitySample Sample)>();

        foreach (var cityGroup in full.GroupBy(x => x.CityId))
        {
            var ordered = cityGroup
                .OrderBy(x => x.Year * 100 + x.Month)
                .ToList();

            // Eğitim örnekleri: her ay için önceki 3 ay feature, o ay label
            for (int i = 3; i < ordered.Count; i++)
            {
                float lag1 = ordered[i - 1].Total;
                float lag2 = ordered[i - 2].Total;
                float lag3 = ordered[i - 3].Total;

                samples.Add(new MonthlyCitySample
                {
                    Lag1 = lag1,
                    Lag2 = lag2,
                    Lag3 = lag3,
                    Avg3 = (lag1 + lag2 + lag3) / 3f,
                    TargetMonth = ordered[i].Month,
                    Label = ordered[i].Total >= Threshold
                });
            }

            // Canlı tahmin girdisi: son 3 tam ay -> gelecek ay
            if (ordered.Count >= 3)
            {
                var last3 = ordered.TakeLast(3).ToList();
                float l1 = last3[2].Total, l2 = last3[1].Total, l3 = last3[0].Total;
                int lastMonth = ordered[^1].Month;
                int nextMonth = lastMonth == 12 ? 1 : lastMonth + 1;

                liveInputs.Add((cityGroup.Key, new MonthlyCitySample
                {
                    Lag1 = l1,
                    Lag2 = l2,
                    Lag3 = l3,
                    Avg3 = (l1 + l2 + l3) / 3f,
                    TargetMonth = nextMonth
                }));
            }
        }

        vm.SampleCount = samples.Count;
        if (samples.Count < 20) return vm; // güvenlik

        // 4) ML.NET'e ver, train/test böl
        var ml = new MLContext(seed: 0);
        IDataView data = ml.Data.LoadFromEnumerable(samples);
        var split = ml.Data.TrainTestSplit(data, testFraction: TestFraction, seed: 0);

        // 5) Pipeline: feature'ları birleştir + normalize + SdcaLogisticRegression
        var pipeline = ml.Transforms
            .Concatenate("Features",
                nameof(MonthlyCitySample.Lag1),
                nameof(MonthlyCitySample.Lag2),
                nameof(MonthlyCitySample.Lag3),
                nameof(MonthlyCitySample.Avg3),
                nameof(MonthlyCitySample.TargetMonth))
            .Append(ml.Transforms.NormalizeMinMax("Features"))
            .Append(ml.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: nameof(MonthlyCitySample.Label),
                featureColumnName: "Features"));

        // 6) Eğit
        var model = pipeline.Fit(split.TrainSet);

        // 7) Test setinde değerlendir
        IDataView scored = model.Transform(split.TestSet);
        var metrics = ml.BinaryClassification.Evaluate(
            scored, labelColumnName: nameof(MonthlyCitySample.Label));

        vm.Accuracy = metrics.Accuracy;
        vm.AreaUnderRoc = metrics.AreaUnderRocCurve;
        vm.F1Score = metrics.F1Score;
        vm.Precision = metrics.PositivePrecision;
        vm.Recall = metrics.PositiveRecall;

        // 8) Confusion matrix'i test setinden KESİN sırayla kendimiz sayalım
        var testRows = ml.Data
            .CreateEnumerable<ThresholdPrediction>(scored, reuseRowObject: false)
            .ToList();

        vm.TestCount = testRows.Count;
        vm.TrainCount = samples.Count - testRows.Count;

        foreach (var r in testRows)
        {
            if (r.Label && r.PredictedLabel) vm.TruePositive++;
            else if (!r.Label && r.PredictedLabel) vm.FalsePositive++;
            else if (!r.Label && !r.PredictedLabel) vm.TrueNegative++;
            else vm.FalseNegative++;
        }

        // 9) Her şehir için gelecek ay tahmini (canlı)
        var engine = ml.Model.CreatePredictionEngine<MonthlyCitySample, ThresholdPrediction>(model);

        foreach (var (cityId, sample) in liveInputs)
        {
            var p = engine.Predict(sample);
            vm.Predictions.Add(new CityThresholdPrediction
            {
                City = cityNames.TryGetValue(cityId, out var n) ? n : $"#{cityId}",
                Lag3 = sample.Lag3,
                Lag2 = sample.Lag2,
                Lag1 = sample.Lag1,
                Avg3 = sample.Avg3,
                WillExceed = p.PredictedLabel,
                Probability = p.Probability
            });
        }

        // En yüksek olasılık üstte
        vm.Predictions = vm.Predictions.OrderByDescending(x => x.Probability).ToList();

        // Gelecek ay etiketi (ilk canlı girdinin hedef ayından)
        if (liveInputs.Count > 0)
        {
            int nm = (int)liveInputs[0].Sample.TargetMonth;
            // hedef ay, son tam ayın bir sonrası; yıl için son tam ayın yılını baz al
            var lastFull = full.OrderByDescending(x => x.Year * 100 + x.Month).First();
            int year = lastFull.Month == 12 ? lastFull.Year + 1 : lastFull.Year;
            var tr = new CultureInfo("tr-TR");
            vm.NextMonthLabel = $"{tr.DateTimeFormat.GetMonthName(nm)} {year}";
        }

        return vm;
    }
}