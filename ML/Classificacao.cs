using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Spectre.Console;
using TradeIA.Models;

namespace TradeIA.ML
{
    public class Classificacao
    {
        private readonly MLContext _mlContext = new();
        private readonly bool treinar;

        public Classificacao(bool treinar = true)
        {
            this.treinar = treinar;
        }

        public PredictionEngine<CandleData, CandleClassificacaoPrediction> Treinar(List<CandleData> candles)
        {
            // 1) Carrega modelo existente se não for retrain
            if (!treinar && File.Exists("Modelos/classificacao.zip"))
            {
                var loaded = _mlContext.Model.Load("Modelos/classificacao.zip", out _);
                return _mlContext.Model.CreatePredictionEngine<CandleData, CandleClassificacaoPrediction>(loaded);
            }

            // 2) Converte para IDataView
            var dataView = _mlContext.Data.LoadFromEnumerable(candles);

            // 3) Bloco de transforms reutilizável
            var baseTransforms = _mlContext.Transforms
                // Label: UP/DOWN → chave numérica
                .Conversion.MapValueToKey("Label", nameof(CandleData.Resultado))

                // Candlestick patterns → float
                .Append(_mlContext.Transforms.Conversion.ConvertType("IsHammerF", nameof(CandleData.IsHammer), DataKind.Single))
                .Append(_mlContext.Transforms.Conversion.ConvertType("IsInvertedHammerF", nameof(CandleData.IsInvertedHammer), DataKind.Single))
                .Append(_mlContext.Transforms.Conversion.ConvertType("IsDojiF", nameof(CandleData.IsDoji), DataKind.Single))
                .Append(_mlContext.Transforms.Conversion.ConvertType("IsBullishEngulfingF", nameof(CandleData.IsBullishEngulfing), DataKind.Single))
                .Append(_mlContext.Transforms.Conversion.ConvertType("IsBearishEngulfingF", nameof(CandleData.IsBearishEngulfing), DataKind.Single))
                .Append(_mlContext.Transforms.Conversion.ConvertType("IsShootingStarF", nameof(CandleData.IsShootingStar), DataKind.Single))
                .Append(_mlContext.Transforms.Conversion.ConvertType("IsMorningStarF", nameof(CandleData.IsMorningStar), DataKind.Single))
                .Append(_mlContext.Transforms.Conversion.ConvertType("IsEveningStarF", nameof(CandleData.IsEveningStar), DataKind.Single))

                // Suporte/Resistência flags → float
                .Append(_mlContext.Transforms.Conversion.ConvertType("RompeuResistenciaF", nameof(CandleData.RompeuResistencia), DataKind.Single))
                .Append(_mlContext.Transforms.Conversion.ConvertType("RompeuSuporteF", nameof(CandleData.RompeuSuporte), DataKind.Single))

                // Concatena todas as features
                .Append(_mlContext.Transforms.Concatenate("Features",
                    nameof(CandleData.Open), nameof(CandleData.High), nameof(CandleData.Low), nameof(CandleData.Close), nameof(CandleData.Volume),
                    "IsHammerF", "IsInvertedHammerF", "IsDojiF", "IsBullishEngulfingF", "IsBearishEngulfingF", "IsShootingStarF", "IsMorningStarF", "IsEveningStarF",
                    nameof(CandleData.DistanciaParaResistencia), nameof(CandleData.DistanciaParaSuporte),
                    "RompeuResistenciaF", "RompeuSuporteF",
                    nameof(CandleData.SMA), nameof(CandleData.EMA), nameof(CandleData.RSI),
                    nameof(CandleData.ATR), nameof(CandleData.BollingerUpper), nameof(CandleData.BollingerLower)
                ));

            // 4) Grid Search: testar L2Regularization e MaximumNumberOfIterations
            double bestAccuracy = 0;
            (float L2, int Iter) bestParams = (0f, 0);

            var l2Tests = new float[] { 0.001f, 0.01f, 0.1f };
            var iterTests = new int[] { 50, 100, 200 };

            foreach (var l2 in l2Tests)
            {
                foreach (var iter in iterTests)
                {
                    // trial pipeline
                    var trial = baseTransforms.Append(
                        _mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(new SdcaMaximumEntropyMulticlassTrainer.Options
                        {
                            LabelColumnName = "Label",
                            FeatureColumnName = "Features",
                            L2Regularization = l2,
                            MaximumNumberOfIterations = iter,
                        }))
                        .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

                    // 5-fold CV
                    var cv = _mlContext.MulticlassClassification.CrossValidate(
                        data: dataView,
                        estimator: trial,
                        numberOfFolds: 5,
                        labelColumnName: "Label"
                    );
                    var micro = cv.Average(r => r.Metrics.MicroAccuracy);

                    if (micro > bestAccuracy)
                    {
                        bestAccuracy = micro;
                        bestParams = (l2, iter);
                    }
                }
            }
            AnsiConsole.MarkupLine($"[yellow]Melhor SDCA → L2={bestParams.L2}, Iter={bestParams.Iter}, Acc={bestAccuracy:P2}[/]");

            // 5) Treina e salva modelo final
            var finalPipeline = baseTransforms.Append(
                _mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(new SdcaMaximumEntropyMulticlassTrainer.Options
                {
                    LabelColumnName = "Label",
                    FeatureColumnName = "Features",
                    L2Regularization = bestParams.L2,
                    MaximumNumberOfIterations = bestParams.Iter,
                }))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            var model = finalPipeline.Fit(dataView);
            Directory.CreateDirectory("Modelos");
            _mlContext.Model.Save(model, dataView.Schema, "Modelos/classificacao.zip");

            return _mlContext.Model.CreatePredictionEngine<CandleData, CandleClassificacaoPrediction>(model);
        }
    }
}
