using Microsoft.ML;
using TradeIA.Models;

namespace TradeIA.ML;

public class Regressao
{
    private readonly MLContext _mlContext = new();

    public PredictionEngine<CandleData, CandleRegressionPrediction> Treinar(List<CandleData> candles)
    {
        var dataView = _mlContext.Data.LoadFromEnumerable(candles);

        var pipeline = _mlContext.Transforms.Concatenate("Features", nameof(CandleData.Open), nameof(CandleData.High), nameof(CandleData.Low), nameof(CandleData.Close), nameof(CandleData.Volume))
            .Append(_mlContext.Regression.Trainers.FastTree(labelColumnName: nameof(CandleData.CloseNext)));

        var model = pipeline.Fit(dataView);

        _mlContext.Model.Save(model, dataView.Schema, "Modelos/regressao.zip");

        return _mlContext.Model.CreatePredictionEngine<CandleData, CandleRegressionPrediction>(model);
    }
}
