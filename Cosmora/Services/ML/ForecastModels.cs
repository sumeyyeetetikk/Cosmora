namespace Cosmora.Services.ML;

// SSA'ya giren tek bir günlük gözlem
public class DailySalesData
{
    public float Quantity { get; set; } //günlük toplam satış saysı
}

// SSA'nın ürettiği tahmin çıktısı (horizon kadar dizi döner)
public class SalesForecastOutput
{
    public float[] ForecastedQuantity { get; set; } = default!;
    public float[] LowerBound { get; set; } = default!;
    public float[] UpperBound { get; set; } = default!;
}