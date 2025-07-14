using Microsoft.ML;
using TradeIA.Models;

namespace TradeIA.ML;

public class Classificacao
{
    private readonly MLContext _mlContext = new();

    public PredictionEngine<CandleData, CandleClassificacaoPrediction> Treinar(List<CandleData> candles)
    {
        var dataView = _mlContext.Data.LoadFromEnumerable(candles);

        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(CandleData.Resultado))
            .Append(_mlContext.Transforms.Concatenate("Features", nameof(CandleData.Open), nameof(CandleData.High), nameof(CandleData.Low), nameof(CandleData.Close), nameof(CandleData.Volume)))
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        var model = pipeline.Fit(dataView);
        _mlContext.Model.Save(model, dataView.Schema, "Modelos/classificacao.zip");

        return _mlContext.Model.CreatePredictionEngine<CandleData, CandleClassificacaoPrediction>(model);
    }
}
