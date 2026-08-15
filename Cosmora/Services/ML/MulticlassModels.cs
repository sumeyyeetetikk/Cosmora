namespace Cosmora.Services.ML;

// Modele giren örnek — feature'lar Binary ile aynı, Label artık string (3 sınıf)
public class MonthlyCityClassSample
{
    public float Lag1 { get; set; }
    public float Lag2 { get; set; }
    public float Lag3 { get; set; }
    public float Avg3 { get; set; }
    public float TargetMonth { get; set; }

    public string Label { get; set; } = "";  // "Low" / "Medium" / "High"
}

// Çıktı
public class PerfPrediction
{
    public string PredictedLabel { get; set; } = "";
    public float[] Score { get; set; } = default!;  // her sınıfın olasılığı (ClassOrder sırasıyla)
}