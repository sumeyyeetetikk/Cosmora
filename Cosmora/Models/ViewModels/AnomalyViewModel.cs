namespace Cosmora.Models.ViewModels;

public class AnomalyViewModel
{
    public int? CityId { get; set; }
    public List<FilterOption> Cities { get; set; } = new();
    public string? SelectedCityName { get; set; }

    public double Threshold { get; set; }
    public double Sensitivity { get; set; }
    public int Period { get; set; }

    public int TotalDays { get; set; }
    public int AnomalyCount { get; set; }

    public List<AnomalyPoint> Series { get; set; } = new();
    public List<AnomalyPoint> Anomalies { get; set; } = new();
}

public class AnomalyPoint
{
    public string Date { get; set; } = "";
    public double Value { get; set; }     
    public double Expected { get; set; }  
    public bool IsAnomaly { get; set; }
    public double Score { get; set; }      // 0-1 anomali skoru
    public string Direction { get; set; } = ""; // "Sıçrama" / "Düşüş"
}