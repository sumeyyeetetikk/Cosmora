using Cosmora.Context;
using Cosmora.Models.ViewModels;
using Cosmora.Services.ML;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

namespace Cosmora.Services;

public class ClusterService : IClusterService
{
    private readonly CosmoraDbContext _db;
    private const int K = 4;

    public ClusterService(CosmoraDbContext db) => _db = db;

    public async Task<ClusterViewModel> RunAsync()
    {
        var vm = new ClusterViewModel { K = K };

        // 1) SQL'de şehir + gün bazında toplam adet.
        //    GroupBy(CityId, OrderDate.Date) SQL'e çevrilebilir; hafta sonu/mevsim
        //    ayrımını burada YAPMIYORUZ (DayOfWeek SQL'e çevrilemiyor).
        //    Sonuç ~30 şehir × ~1095 gün = küçük, belleğe rahat sığar (1M değil).
        var daily = await _db.Sales
            .GroupBy(s => new { s.CityId, Day = s.OrderDate.Date })
            .Select(g => new
            {
                g.Key.CityId,
                g.Key.Day,
                Qty = g.Sum(x => (long)x.Quantity)
            })
            .ToListAsync();

        if (daily.Count == 0) return vm;

        var cityInfo = await _db.Cities
            .ToDictionaryAsync(c => c.Id, c => new { c.Name, c.Country });

        // 2) Şehir başına hacim temelli feature'ları BELLEKTE hesapla.
        //    Bu veri setinde şehirleri ayıran temel faktör hacim; yaz payı/hafta sonu
        //    tüm şehirlerde ~aynı çıktığı için kümeleme yapmıyordu.
        var featureList = new List<CityFeatures>();
        var meta = new List<(int CityId, double AvgDaily, double TotalVolume, double PeakDay)>();

        foreach (var cityGroup in daily.GroupBy(d => d.CityId))
        {
            var days = cityGroup.ToList();

            long totalQty = days.Sum(d => d.Qty);
            int distinctDays = days.Count;
            double avgDaily = distinctDays > 0 ? (double)totalQty / distinctDays : 0;
            double peakDay = days.Count > 0 ? days.Max(d => (double)d.Qty) : 0;

            featureList.Add(new CityFeatures
            {
                AvgDailySales = (float)avgDaily,
                TotalVolume = (float)totalQty,
                PeakDaySales = (float)peakDay
            });
            meta.Add((cityGroup.Key, avgDaily, totalQty, peakDay));
        }

        vm.CityCount = featureList.Count;

        // 3) ML.NET: normalize + K-Means
        var ml = new MLContext(seed: 0);
        IDataView data = ml.Data.LoadFromEnumerable(featureList);

        var pipeline = ml.Transforms
            .Concatenate("Features",
                nameof(CityFeatures.AvgDailySales),
                nameof(CityFeatures.TotalVolume),
                nameof(CityFeatures.PeakDaySales))
            .Append(ml.Transforms.NormalizeMinMax("Features"))
            .Append(ml.Clustering.Trainers.KMeans("Features", numberOfClusters: K));

        var model = pipeline.Fit(data);
        var engine = ml.Model.CreatePredictionEngine<CityFeatures, CityClusterPrediction>(model);

        for (int i = 0; i < featureList.Count; i++)
        {
            var pred = engine.Predict(featureList[i]);
            var m = meta[i];
            var info = cityInfo.TryGetValue(m.CityId, out var ci) ? ci : null;

            vm.Cities.Add(new CityClusterRow
            {
                City = info?.Name ?? $"#{m.CityId}",
                Country = info?.Country ?? "",
                Cluster = (int)pred.PredictedClusterId,
                AvgDailySales = m.AvgDaily,
                TotalVolume = m.TotalVolume,
                PeakDaySales = m.PeakDay
            });
        }

        // 4) Küme özetleri + otomatik yorum
        double allAvgDaily = vm.Cities.Average(x => x.AvgDailySales);

        vm.Clusters = vm.Cities
            .GroupBy(x => x.Cluster)
            .Select(g =>
            {
                var s = new ClusterSummary
                {
                    Cluster = g.Key,
                    CityCount = g.Count(),
                    AvgDailySales = g.Average(x => x.AvgDailySales),
                    TotalVolume = g.Average(x => x.TotalVolume),
                    PeakDaySales = g.Average(x => x.PeakDaySales),
                    CityNames = g.OrderByDescending(x => x.AvgDailySales)
                                 .Select(x => x.City).ToList()
                };
                s.Label = BuildLabel(s, allAvgDaily);
                return s;
            })
            .OrderByDescending(c => c.AvgDailySales)
            .ToList();

        return vm;
    }

    // Kümeye ortalama günlük satış hacmine göre okunabilir bir etiket üret
    private static string BuildLabel(ClusterSummary s, double avgDaily)
    {
        return s.AvgDailySales >= avgDaily * 1.4 ? "Metropoller (çok yüksek hacim)"
             : s.AvgDailySales >= avgDaily * 1.05 ? "Büyük şehirler (yüksek hacim)"
             : s.AvgDailySales >= avgDaily * 0.7 ? "Orta ölçekli şehirler"
             : "Küçük şehirler (düşük hacim)";
    }
}