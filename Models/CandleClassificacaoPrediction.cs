using Microsoft.ML.Data;

namespace TradeIA.Models;

public class CandleClassificacaoPrediction
{
    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; set; }
    public float[] Score { get; set; }
    public string Resultado => PredictedLabel;
    public float Confiança => Score?.Max() ?? 0f;
}
