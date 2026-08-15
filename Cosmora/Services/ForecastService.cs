using Cosmora.Context;
using Cosmora.Models.ViewModels;
using Cosmora.Services.ML;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace Cosmora.Services;

public class ForecastService : IForecastService
{
    private readonly CosmoraDbContext _db;

    // Case parametreleri
    private const int WindowSize = 7;
    private const int SeriesLength = 30;
    private const int Horizon = 7;
    private const float ConfidenceLevel = 0.95f;

    public ForecastService(CosmoraDbContext db) => _db = db;

    public async Task<List<FilterOption>> GetCitiesAsync() =>
        await _db.Cities.OrderBy(c => c.Name)
            .Select(c => new FilterOption { Id = c.Id, Name = $"{c.Name} ({c.Country})" })
            .ToListAsync();

    public async Task<ForecastViewModel> ForecastAsync(int cityId)
    {
        // 1) SQL'de günlük toplam adet (tüm seri belleğe değil, sadece günlük özet gelir)
        var daily = await _db.Sales
            .Where(s => s.CityId == cityId)
            .GroupBy(s => s.OrderDate.Date)
            .Select(g => new { Date = g.Key, Qty = g.Sum(x => (long)x.Quantity) })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var cityName = await _db.Cities.Where(c => c.Id == cityId)
            .Select(c => c.Name).FirstOrDefaultAsync();

        var vm = new ForecastViewModel
        {
            CityId = cityId,
            SelectedCityName = cityName,
            WindowSize = WindowSize,
            SeriesLength = SeriesLength,
            TrainSize = daily.Count,
            Horizon = Horizon,
            ConfidenceLevel = ConfidenceLevel
        };

        if (daily.Count < SeriesLength + Horizon)
            return vm; // yeterli veri yoksa boş dön

        // 2) ML.NET veri yapısına çevir
        var mlContext = new MLContext(seed: 0);
        var series = daily.Select(d => new DailySalesData { Quantity = d.Qty }).ToList();
        IDataView dataView = mlContext.Data.LoadFromEnumerable(series);

        // 3) SSA pipeline'ı MANUEL kur (case: Model Builder yok)
        var pipeline = mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(SalesForecastOutput.ForecastedQuantity),
            inputColumnName: nameof(DailySalesData.Quantity),
            windowSize: WindowSize,
            seriesLength: SeriesLength,
            trainSize: daily.Count,
            horizon: Horizon,
            confidenceLevel: ConfidenceLevel,
            confidenceLowerBoundColumn: nameof(SalesForecastOutput.LowerBound),
            confidenceUpperBoundColumn: nameof(SalesForecastOutput.UpperBound));

        // 4) Eğit ve tahmin et
        var model = pipeline.Fit(dataView);
        var engine = model.CreateTimeSeriesEngine<DailySalesData, SalesForecastOutput>(mlContext);
        var prediction = engine.Predict();

        // 5) Son 30 gün gerçek veriyi grafik için al
        var last30 = daily.Skip(Math.Max(0, daily.Count - 30)).ToList();
        vm.History = last30.Select(d => new HistoryPoint
        {
            Date = d.Date.ToString("dd.MM"),
            Quantity = d.Qty
        }).ToList();

        // 6) Tahmini son güne ekleyerek 7 günlük ileri tarih üret
        var lastDate = daily.Last().Date;
        for (int i = 0; i < Horizon; i++)
        {
            vm.Forecast.Add(new ForecastPoint
            {
                Date = lastDate.AddDays(i + 1).ToString("dd.MM"),
                Predicted = MathF.Max(0, prediction.ForecastedQuantity[i]),
                LowerBound = MathF.Max(0, prediction.LowerBound[i]),
                UpperBound = MathF.Max(0, prediction.UpperBound[i])
            });
        }

        return vm;
    }
}