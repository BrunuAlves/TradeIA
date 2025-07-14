using Microsoft.ML.Data;

namespace TradeIA.Models;

public class CandleRegressionPrediction
{
    [ColumnName("Score")]
    public float PredictedCloseNext;
}
