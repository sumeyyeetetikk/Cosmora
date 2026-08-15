using System.Globalization;
using Cosmora.Context;
using Cosmora.Models.ViewModels;
using Cosmora.Services.ML;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Cosmora.Services;

public class MulticlassService : IMulticlassService
{
    private readonly CosmoraDbContext _db;
    private const double TestFraction = 0.2;

    public MulticlassService(CosmoraDbContext db) => _db = db;

    public async Task<MulticlassViewModel> RunAsync()
    {
        var vm = new MulticlassViewModel();

        // 1) (şehir, yıl, ay) aylık toplam — SQL tarafında
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

        var cityNames = await _db.Cities.ToDictionaryAsync(c => c.Id, c => c.Name);

        // 2) Yarım ilk/son ayı çıkar
        int minKey = monthly.Min(x => x.Year * 100 + x.Month);
        int maxKey = monthly.Max(x => x.Year * 100 + x.Month);
        var full = monthly
            .Where(x => (x.Year * 100 + x.Month) != minKey
                     && (x.Year * 100 + x.Month) != maxKey)
            .ToList();

        // 3) Önce feature + hedef toplamı biriktir (sınıfı HENÜZ atamıyoruz)
        var raw = new List<(float Lag1, float Lag2, float Lag3, float Avg3, int Month, long Total)>();
        var liveInputs = new List<(int CityId, MonthlyCityClassSample Sample)>();

        foreach (var cityGroup in full.GroupBy(x => x.CityId))
        {
            var ordered = cityGroup.OrderBy(x => x.Year * 100 + x.Month).ToList();

            for (int i = 3; i < ordered.Count; i++)
            {
                float lag1 = ordered[i - 1].Total;
                float lag2 = ordered[i - 2].Total;
                float lag3 = ordered[i - 3].Total;
                raw.Add((lag1, lag2, lag3, (lag1 + lag2 + lag3) / 3f,
                         ordered[i].Month, ordered[i].Total));
            }

            if (ordered.Count >= 3)
            {
                var last3 = ordered.TakeLast(3).ToList();
                float l1 = last3[2].Total, l2 = last3[1].Total, l3 = last3[0].Total;
                int lastMonth = ordered[^1].Month;
                int nextMonth = lastMonth == 12 ? 1 : lastMonth + 1;
                liveInputs.Add((cityGroup.Key, new MonthlyCityClassSample
                {
                    Lag1 = l1,
                    Lag2 = l2,
                    Lag3 = l3,
                    Avg3 = (l1 + l2 + l3) / 3f,
                    TargetMonth = nextMonth
                }));
            }
        }

        if (raw.Count < 30) { vm.SampleCount = raw.Count; return vm; }

        // 4) SINIF SINIRLARI: hedef toplamların %33 ve %66 persentili
        var sortedTotals = raw.Select(r => r.Total).OrderBy(x => x).ToList();
        float Percentile(double q)
        {
            int idx = (int)Math.Round(q * (sortedTotals.Count - 1));
            return sortedTotals[Math.Clamp(idx, 0, sortedTotals.Count - 1)];
        }
        float p33 = Percentile(0.33);
        float p66 = Percentile(0.66);
        vm.P33 = p33; vm.P66 = p66;
        vm.LowUpper = p33; vm.MediumUpper = p66;

        string ClassOf(long total) =>
            total <= p33 ? "Low" : total <= p66 ? "Medium" : "High";

        // 5) Sınıf etiketlerini ata
        var samples = raw.Select(r => new MonthlyCityClassSample
        {
            Lag1 = r.Lag1,
            Lag2 = r.Lag2,
            Lag3 = r.Lag3,
            Avg3 = r.Avg3,
            TargetMonth = r.Month,
            Label = ClassOf(r.Total)
        }).ToList();

        vm.SampleCount = samples.Count;
        vm.LowCount = samples.Count(s => s.Label == "Low");
        vm.MediumCount = samples.Count(s => s.Label == "Medium");
        vm.HighCount = samples.Count(s => s.Label == "High");

        // 6) ML.NET: train/test böl
        var ml = new MLContext(seed: 0);
        IDataView data = ml.Data.LoadFromEnumerable(samples);
        var split = ml.Data.TrainTestSplit(data, testFraction: TestFraction, seed: 0);

        // 7) Pipeline: Label->key, feature birleştir+normalize, SdcaMaximumEntropy, key->value
        var pipeline = ml.Transforms.Conversion
            .MapValueToKey("Label", nameof(MonthlyCityClassSample.Label))
            .Append(ml.Transforms.Concatenate("Features",
                nameof(MonthlyCityClassSample.Lag1),
                nameof(MonthlyCityClassSample.Lag2),
                nameof(MonthlyCityClassSample.Lag3),
                nameof(MonthlyCityClassSample.Avg3),
                nameof(MonthlyCityClassSample.TargetMonth)))
            .Append(ml.Transforms.NormalizeMinMax("Features"))
            .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: "Label", featureColumnName: "Features"))
            .Append(ml.Transforms.Conversion.MapKeyToValue(
                "PredictedLabel", "PredictedLabel"));

        var model = pipeline.Fit(split.TrainSet);

        // 8) Test setinde değerlendir
        IDataView scored = model.Transform(split.TestSet);
        var metrics = ml.MulticlassClassification.Evaluate(scored, labelColumnName: "Label");
        vm.MicroAccuracy = metrics.MicroAccuracy;
        vm.MacroAccuracy = metrics.MacroAccuracy;
        vm.LogLoss = metrics.LogLoss;

        // 9) Sınıfların gerçek sırası (Score kolonunun slot adları = key sırası)
        VBuffer<ReadOnlyMemory<char>> slotBuffer = default;
        scored.Schema["Score"].GetSlotNames(ref slotBuffer);
        vm.ClassOrder = slotBuffer.DenseValues().Select(v => v.ToString()).ToList();

        // 10) Confusion matrix (ClassOrder sırasıyla)
        int k = vm.ClassOrder.Count;
        var cm = metrics.ConfusionMatrix;
        vm.Confusion = new long[k][];
        for (int i = 0; i < k; i++)
        {
            vm.Confusion[i] = new long[k];
            for (int j = 0; j < k; j++)
                vm.Confusion[i][j] = (long)cm.Counts[i][j];
        }

        vm.TestCount = (int)vm.Confusion.Sum(row => row.Sum());
        vm.TrainCount = samples.Count - vm.TestCount;

        // 11) Gelecek ay tahminleri
        var engine = ml.Model.CreatePredictionEngine<MonthlyCityClassSample, PerfPrediction>(model);
        foreach (var (cityId, sample) in liveInputs)
        {
            var p = engine.Predict(sample);
            vm.Predictions.Add(new CityPerfPrediction
            {
                City = cityNames.TryGetValue(cityId, out var n) ? n : $"#{cityId}",
                Lag1 = sample.Lag1,
                Avg3 = sample.Avg3,
                PredictedClass = p.PredictedLabel,
                Confidence = p.Score is { Length: > 0 } ? p.Score.Max() : 0f
            });
        }
        vm.Predictions = vm.Predictions
            .OrderByDescending(x => x.Avg3).ToList();

        // Gelecek ay etiketi
        if (liveInputs.Count > 0)
        {
            var lastFull = full.OrderByDescending(x => x.Year * 100 + x.Month).First();
            int nm = (int)liveInputs[0].Sample.TargetMonth;
            int year = lastFull.Month == 12 ? lastFull.Year + 1 : lastFull.Year;
            var tr = new CultureInfo("tr-TR");
            vm.NextMonthLabel = $"{tr.DateTimeFormat.GetMonthName(nm)} {year}";
        }

        return vm;
    }
}