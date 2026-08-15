namespace Cosmora.Models.ViewModels;

public class BinaryViewModel
{
    public int Threshold { get; set; } = 7000;

    // Veri hacmi
    public int SampleCount { get; set; }
    public int TrainCount { get; set; }
    public int TestCount { get; set; }

    // Metrikler (test seti üzerinde)
    public double Accuracy { get; set; }
    public double AreaUnderRoc { get; set; }
    public double F1Score { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }

    // Confusion matrix (test seti)
    public long TruePositive { get; set; }
    public long FalsePositive { get; set; }
    public long TrueNegative { get; set; }
    public long FalseNegative { get; set; }

    // Her şehir için gelecek ay tahmini (canlı)
    public string NextMonthLabel { get; set; } = "";   // ör. "Eylül 2026"
    public List<CityThresholdPrediction> Predictions { get; set; } = new();
}

public class CityThresholdPrediction
{
    public string City { get; set; } = "";
    public float Lag3 { get; set; }   // 3 ay önce
    public float Lag2 { get; set; }   // 2 ay önce
    public float Lag1 { get; set; }   // geçen ay
    public float Avg3 { get; set; }
    public bool WillExceed { get; set; }
    public float Probability { get; set; }
}