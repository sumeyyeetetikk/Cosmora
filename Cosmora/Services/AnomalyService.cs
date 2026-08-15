using Cosmora.Context;
using Cosmora.Models.ViewModels;
using Cosmora.Services.ML;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.TimeSeries;

namespace Cosmora.Services;

public class AnomalyService : IAnomalyService
{
    private readonly CosmoraDbContext _db;

    // Case parametreleri
    private const double Threshold = 0.15;   // skor bu eşiği aşarsa anomali
    private const double Sensitivity = 90.0; // margin hesabı (yüksek = dar bant)
    private const int Period = 7;            // haftalık mevsimsellik periyodu

    public AnomalyService(CosmoraDbContext db) => _db = db;

    public async Task<List<FilterOption>> GetCitiesAsync() =>
        await _db.Cities.OrderBy(c => c.Name)
            .Select(c => new FilterOption { Id = c.Id, Name = $"{c.Name} ({c.Country})" })
            .ToListAsync();

    public async Task<AnomalyViewModel> DetectAsync(int cityId)
    {
        // 1) SQL'de günlük toplam adet (küçük seri, ~1095 gün)
        var daily = await _db.Sales
            .Where(s => s.CityId == cityId)
            .GroupBy(s => s.OrderDate.Date)
            .Select(g => new { Date = g.Key, Qty = g.Sum(x => (long)x.Quantity) })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var cityName = await _db.Cities.Where(c => c.Id == cityId)
            .Select(c => c.Name).FirstOrDefaultAsync();

        var vm = new AnomalyViewModel
        {
            CityId = cityId,
            SelectedCityName = cityName,
            Threshold = Threshold,
            Sensitivity = Sensitivity,
            Period = Period,
            TotalDays = daily.Count
        };

        if (daily.Count < 4 * Period) return vm; // SR-CNN için yeterli veri yok

        // 2) ML.NET veri yapısına çevir (double!)
        var ml = new MLContext(seed: 0);
        var points = daily.Select(d => new DailySalesPoint { Value = d.Qty }).ToList();
        IDataView dataView = ml.Data.LoadFromEnumerable(points);

        // 3) SR-CNN — parametreleri Options nesnesiyle ver (versiyon-bağımsız overload)
        var options = new SrCnnEntireAnomalyDetectorOptions
        {
            Threshold = Threshold,
            BatchSize = -1,              // -1 = tüm seri tek batch
            Sensitivity = Sensitivity,
            DetectMode = SrCnnDetectMode.AnomalyAndMargin,
            Period = Period
        };

        IDataView transformed = ml.AnomalyDetection.DetectEntireAnomalyBySrCnn(
            dataView,
            outputColumnName: nameof(SrCnnAnomalyOutput.Prediction),
            inputColumnName: nameof(DailySalesPoint.Value),
            options);

        // 4) Sonuçları oku ve tarihlerle eşle
        var results = ml.Data
            .CreateEnumerable<SrCnnAnomalyOutput>(transformed, reuseRowObject: false)
            .ToList();

        for (int i = 0; i < results.Count; i++)
        {
            var p = results[i].Prediction;
            bool isAnomaly = p[0] == 1;
            double value = daily[i].Qty;
            double expected = p[3];

            var point = new AnomalyPoint
            {
                Date = daily[i].Date.ToString("dd.MM.yy"),
                Value = value,
                Expected = expected,
                IsAnomaly = isAnomaly,
                Score = p[1],
                Direction = isAnomaly
                    ? (value >= expected ? "Sıçrama" : "Düşüş")
                    : ""
            };

            vm.Series.Add(point);
            if (isAnomaly) vm.Anomalies.Add(point);
        }

        vm.AnomalyCount = vm.Anomalies.Count;
        // Tabloda en yüksek skorlu anomaliler üstte
        vm.Anomalies = vm.Anomalies.OrderByDescending(a => a.Score).ToList();

        return vm;
    }
}