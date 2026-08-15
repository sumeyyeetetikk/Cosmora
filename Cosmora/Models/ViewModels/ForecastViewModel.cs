namespace Cosmora.Models.ViewModels;

public class ForecastViewModel
{
    public int? CityId { get; set; }
    public List<FilterOption> Cities { get; set; } = new();

    // Grafik için: son 30 gün gerçek + 7 gün tahmin
    public List<HistoryPoint> History { get; set; } = new();
    public List<ForecastPoint> Forecast { get; set; } = new();

    // Kullanılan parametreleri ekranda göstereceğiz
    public int WindowSize { get; set; }
    public int SeriesLength { get; set; }
    public int TrainSize { get; set; }
    public int Horizon { get; set; }
    public float ConfidenceLevel { get; set; }

    public string? SelectedCityName { get; set; }
}

public class HistoryPoint
{
    public string Date { get; set; } = "";
    public float Quantity { get; set; }
}

public class ForecastPoint
{
    public string Date { get; set; } = "";
    public float Predicted { get; set; }
    public float LowerBound { get; set; }
    public float UpperBound { get; set; }
}