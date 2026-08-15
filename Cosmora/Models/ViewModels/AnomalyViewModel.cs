namespace Cosmora.Models.ViewModels;

public class AnomalyViewModel
{
    public int? CityId { get; set; }
    public List<FilterOption> Cities { get; set; } = new();
    public string? SelectedCityName { get; set; }

    // Kullanılan parametreler (case: açıklanmalı)
    public double Threshold { get; set; }
    public double Sensitivity { get; set; }
    public int Period { get; set; }

    public int TotalDays { get; set; }
    public int AnomalyCount { get; set; }

    // Grafik için tüm seri
    public List<AnomalyPoint> Series { get; set; } = new();
    // Tablo için sadece anomali günleri
    public List<AnomalyPoint> Anomalies { get; set; } = new();
}

public class AnomalyPoint
{
    public string Date { get; set; } = "";
    public double Value { get; set; }      // gerçek günlük satış
    public double Expected { get; set; }   // modelin beklediği değer
    public bool IsAnomaly { get; set; }
    public double Score { get; set; }      // 0-1 anomali skoru
    public string Direction { get; set; } = ""; // "Sıçrama" / "Düşüş"
}