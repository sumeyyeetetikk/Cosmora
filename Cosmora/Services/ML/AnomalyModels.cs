using Microsoft.ML.Data;

namespace Cosmora.Services.ML;

// SR-CNN'e giren tek bir günlük gözlem — DİKKAT: double (float değil)
public class DailySalesPoint
{
    public double Value { get; set; }
}

// SR-CNN çıktısı: AnomalyAndMargin modunda 7 elemanlı vektör
//  [0]=IsAnomaly(0/1) [1]=AnomalyScore [2]=Magnitude
//  [3]=ExpectedValue  [4]=BoundaryUnit [5]=UpperBoundary [6]=LowerBoundary
public class SrCnnAnomalyOutput
{
    [VectorType(7)]
    public double[] Prediction { get; set; } = default!;
}