using Microsoft.ML.Data;

namespace TradeIA.Models;

public class CandleClassificacaoPrediction
{
    [ColumnName("PredictedLabel")]
    public string? Resultado { get; set; }
}
