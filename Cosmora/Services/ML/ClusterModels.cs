using Microsoft.ML.Data;

namespace Cosmora.Services.ML;

public class CityFeatures
{
    public float AvgDailySales { get; set; }
    public float TotalVolume { get; set; }
    public float PeakDaySales { get; set; }
}

public class CityClusterPrediction
{
    [ColumnName("PredictedLabel")]
    public uint PredictedClusterId { get; set; }

    [ColumnName("Score")]
    public float[] Distances { get; set; } = default!;
}