using Microsoft.ML.Data;

namespace Cosmora.Services.ML;

// Modele giren tek bir (şehir, hedef ay) örneği
public class MonthlyCitySample
{
    public float Lag1 { get; set; }        // 1 ay önceki toplam
    public float Lag2 { get; set; }        // 2 ay önce
    public float Lag3 { get; set; }        // 3 ay önce
    public float Avg3 { get; set; }        // son 3 ay ortalaması
    public float TargetMonth { get; set; } // hedef ay (1-12)

    public bool Label { get; set; }        // hedef ay >= 7000 mı
}

// Modelin ürettiği tahmin. Label alanını da tutuyoruz ki test setinde
// gerçek/tahmin karşılaştırıp confusion matrix'i kesin sırayla kuralım.
public class ThresholdPrediction
{
    public bool Label { get; set; }        // gerçek değer (test setinden gelir)

    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; } // modelin tahmini (EVET/HAYIR)

    public float Probability { get; set; }   // 0-1 arası olasılık
    public float Score { get; set; }
}