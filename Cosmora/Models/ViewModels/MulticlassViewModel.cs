namespace Cosmora.Models.ViewModels;

public class MulticlassViewModel
{
    // Veriden hesaplanan sınıf sınırları
    public float LowUpper { get; set; }     // Low:  total <= LowUpper
    public float MediumUpper { get; set; }  // Medium: LowUpper < total <= MediumUpper, High: >
    public float P33 { get; set; }          // %33 persentil (ham)
    public float P66 { get; set; }          // %66 persentil (ham)

    // Veri hacmi
    public int SampleCount { get; set; }
    public int TrainCount { get; set; }
    public int TestCount { get; set; }

    // Eğitim seti sınıf dağılımı
    public int LowCount { get; set; }
    public int MediumCount { get; set; }
    public int HighCount { get; set; }

    // Metrikler (test seti)
    public double MicroAccuracy { get; set; }
    public double MacroAccuracy { get; set; }
    public double LogLoss { get; set; }

    // Confusion matrix (test) — ClassOrder sırasıyla
    public List<string> ClassOrder { get; set; } = new();
    public long[][] Confusion { get; set; } = System.Array.Empty<long[]>();

    // Gelecek ay tahminleri
    public string NextMonthLabel { get; set; } = "";
    public List<CityPerfPrediction> Predictions { get; set; } = new();
}

public class CityPerfPrediction
{
    public string City { get; set; } = "";
    public float Lag1 { get; set; }
    public float Avg3 { get; set; }
    public string PredictedClass { get; set; } = "";
    public float Confidence { get; set; }   // en yüksek sınıf olasılığı
}