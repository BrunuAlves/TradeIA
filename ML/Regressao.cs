// TradeIA/ML/Regressao.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using Spectre.Console;
using TradeIA.Models;

namespace TradeIA.ML
{
    public class Regressao
    {
        private readonly MLContext _mlContext = new();
        private readonly bool treinar;

        public Regressao(bool treinar = true)
        {
            this.treinar = treinar;
        }

        public PredictionEngine<CandleData, CandleRegressionPrediction> Treinar(List<CandleData> candles)
        {
            // 1) Carrega modelo existente se não for retrain
            if (!treinar && File.Exists("Modelos/regressao.zip"))
            {
                var loaded = _mlContext.Model.Load("Modelos/regressao.zip", out _);
                return _mlContext.Model.CreatePredictionEngine<CandleData, CandleRegressionPrediction>(loaded);
            }

            // 2) Converte lista em IDataView
            var dataView = _mlContext.Data.LoadFromEnumerable(candles);

            // 3) Bloco de transforms reutilizável
            var baseTransforms = _mlContext.Transforms
                // Candlestick patterns → float
                .Conversion.ConvertType("IsHammerF", nameof(CandleData.IsHammer), DataKind.Single)
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
                    nameof(CandleData.Open),
                    nameof(CandleData.High),
                    nameof(CandleData.Low),
                    nameof(CandleData.Close),
                    nameof(CandleData.Volume),

                    "IsHammerF",
                    "IsInvertedHammerF",
                    "IsDojiF",
                    "IsBullishEngulfingF",
                    "IsBearishEngulfingF",
                    "IsShootingStarF",
                    "IsMorningStarF",
                    "IsEveningStarF",

                    nameof(CandleData.DistanciaParaResistencia),
                    nameof(CandleData.DistanciaParaSuporte),

                    "RompeuResistenciaF",
                    "RompeuSuporteF",

                    nameof(CandleData.SMA),
                    nameof(CandleData.EMA),
                    nameof(CandleData.RSI),

                    nameof(CandleData.ATR),
                    nameof(CandleData.BollingerUpper),
                    nameof(CandleData.BollingerLower)
                ));

            // 4) Grid Search: testar combinações de FastTree
            int[] testTrees = { 50, 100, 200 };
            float[] testLrs = { 0.05f, 0.1f, 0.2f };
            double bestRmse = double.MaxValue;
            (int trees, float lr) bestParams = (0, 0);

            foreach (var nt in testTrees)
            {
                foreach (var lr in testLrs)
                {
                    // Pipeline de teste
                    var trial = baseTransforms.Append(
                        _mlContext.Regression.Trainers.FastTree(new FastTreeRegressionTrainer.Options
                        {
                            LabelColumnName = nameof(CandleData.CloseNext),
                            FeatureColumnName = "Features",
                            NumberOfTrees = nt,
                            LearningRate = lr
                        }));

                    // 5‑fold CV
                    var cv = _mlContext.Regression.CrossValidate(
                        data: dataView,
                        estimator: trial,
                        numberOfFolds: 5,
                        labelColumnName: nameof(CandleData.CloseNext));
                    var rmse = cv.Average(r => r.Metrics.RootMeanSquaredError);

                    if (rmse < bestRmse)
                        bestRmse = rmse;
                    bestParams = (nt, lr);
                }
            }

            AnsiConsole.MarkupLine($"[yellow]Melhor FastTree → Trees={bestParams.trees}, LR={bestParams.lr}, RMSE={bestRmse:F4}[/]");

            // 5) Pipeline final com melhores parâmetros
            var finalPipeline = baseTransforms.Append(
                _mlContext.Regression.Trainers.FastTree(new FastTreeRegressionTrainer.Options
                {
                    LabelColumnName = nameof(CandleData.CloseNext),
                    FeatureColumnName = "Features",
                    NumberOfTrees = bestParams.trees,
                    LearningRate = bestParams.lr
                }));

            // 6) Treina e salva modelo
            var model = finalPipeline.Fit(dataView);
            Directory.CreateDirectory("Modelos");
            _mlContext.Model.Save(model, dataView.Schema, "Modelos/regressao.zip");

            return _mlContext.Model.CreatePredictionEngine<CandleData, CandleRegressionPrediction>(model);
        }
    }
}
