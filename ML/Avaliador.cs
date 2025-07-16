using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using Spectre.Console;
using TradeIA.Models;

namespace TradeIA.ML
{
    public class Avaliador
    {
        private readonly Regressao _regressao;
        private readonly Classificacao _classificacao;

        public Avaliador(bool treinar = true)
        {
            _regressao = new Regressao(treinar);
            _classificacao = new Classificacao(treinar);
        }

        /// <summary>
        /// Avalia os modelos no conjunto fornecido (regressão vs classificação) e exibe métricas.
        /// </summary>
        public void AvaliarModelos(List<CandleData> candles)
        {
            int total = candles.Count;
            int acertosReg = 0, acertosClass = 0, ambos = 0;
            int concordam = 0, confiantes = 0, acertConfiantes = 0;

            var engineReg = _regressao.Treinar(candles);
            var engineClass = _classificacao.Treinar(candles);

            foreach (var c in candles)
            {
                var pr = engineReg.Predict(c);
                var pc = engineClass.Predict(c);
                bool upR = pr.PredictedCloseNext > c.Close;
                bool upC = pc.Resultado == "UP";
                bool real = c.Resultado == "UP";

                if (upR == real) acertosReg++;
                if (upC == real) acertosClass++;
                if (upR == upC)
                {
                    ambos++;
                    if (upR == real) concordam++;
                    float conf = pc.Score?.Max() ?? 0;
                    float var = Math.Abs((pr.PredictedCloseNext - c.Close) / c.Close);
                    if (conf >= 0.6f && var >= 0.0005f)
                    {
                        confiantes++;
                        if (upC == real) acertConfiantes++;
                    }
                }
            }

            double pctReg = acertosReg * 100.0 / total;
            double pctClass = acertosClass * 100.0 / total;
            double pctAmbos = ambos * 100.0 / total;
            double pctConcord = ambos > 0 ? concordam * 100.0 / ambos : 0;
            double pctConf = confiantes * 100.0 / total;
            double pctAcCo = confiantes > 0 ? acertConfiantes * 100.0 / confiantes : 0;

            // Cabeçalho
            AnsiConsole.Write(new Rule("📊 AVALIAÇÃO FINAL").RuleStyle("yellow").Centered());
            AnsiConsole.MarkupLine($"[green]Total:[/] {total}");

            // Gráfico de barras
            var chart = new BarChart()
                .Width(60)
                .Label("Precisão (%)")
                .CenterLabel()
                .UseValueFormatter(v => $"{v:00.00}%");

            chart.AddItem($"Regressão ({acertosReg})", (float)pctReg, ConsoleColor.Blue);
            chart.AddItem($"Classificação ({acertosClass})", (float)pctClass, ConsoleColor.Green);
            chart.AddItem($"Ambos concordam ({ambos})", (float)pctAmbos, ConsoleColor.Yellow);
            chart.AddItem($"Acertos concordância ({concordam})", (float)pctConcord, ConsoleColor.Magenta);
            chart.AddItem($"Alta confiança ({confiantes})", (float)pctConf, ConsoleColor.Cyan);
            chart.AddItem($"Acertos confiança ({acertConfiantes})", (float)pctAcCo, ConsoleColor.DarkMagenta);

            // Envolvendo em painel (caixa)
            var painel = new Panel(chart)
                .Border(BoxBorder.Rounded)
                .Header("[white] Resultados [/]")
                .Padding(1, 1)
                .Expand();

            AnsiConsole.Write(painel);
        }

        /// <summary>
        /// Validação rolling window (walk‑forward) para regressão, exibindo RMSE e R² médios.
        /// </summary>
        public void WalkForwardValidation(List<CandleData> candles, int windowSize, int testSize)
        {
            var mlContext = new MLContext();
            var rmses = new List<double>();
            var r2s = new List<double>();

            for (int start = 0; start + windowSize + testSize <= candles.Count; start += testSize)
            {
                var train = candles.Skip(start).Take(windowSize).ToList();
                var test = candles.Skip(start + windowSize).Take(testSize).ToList();

                var trainDv = mlContext.Data.LoadFromEnumerable(train);
                var testDv = mlContext.Data.LoadFromEnumerable(test);

                // Pipeline: mesmos transforms de Regressao.cs
                var pipeline = mlContext.Transforms
                    // Conversão de padrões de candle → float
                    .Conversion.ConvertType("IsHammerF", nameof(CandleData.IsHammer), DataKind.Single)
                    .Append(mlContext.Transforms.Conversion.ConvertType("IsInvertedHammerF", nameof(CandleData.IsInvertedHammer), DataKind.Single))
                    .Append(mlContext.Transforms.Conversion.ConvertType("IsDojiF", nameof(CandleData.IsDoji), DataKind.Single))
                    .Append(mlContext.Transforms.Conversion.ConvertType("IsBullishEngulfingF", nameof(CandleData.IsBullishEngulfing), DataKind.Single))
                    .Append(mlContext.Transforms.Conversion.ConvertType("IsBearishEngulfingF", nameof(CandleData.IsBearishEngulfing), DataKind.Single))
                    .Append(mlContext.Transforms.Conversion.ConvertType("IsShootingStarF", nameof(CandleData.IsShootingStar), DataKind.Single))
                    .Append(mlContext.Transforms.Conversion.ConvertType("IsMorningStarF", nameof(CandleData.IsMorningStar), DataKind.Single))
                    .Append(mlContext.Transforms.Conversion.ConvertType("IsEveningStarF", nameof(CandleData.IsEveningStar), DataKind.Single))

                    // Conversão de flags de suporte/resistência → float
                    .Append(mlContext.Transforms.Conversion.ConvertType("RompeuResistenciaF", nameof(CandleData.RompeuResistencia), DataKind.Single))
                    .Append(mlContext.Transforms.Conversion.ConvertType("RompeuSuporteF", nameof(CandleData.RompeuSuporte), DataKind.Single))

                    // Concatenação de todas as features
                    .Append(mlContext.Transforms.Concatenate("Features",
                        nameof(CandleData.Open),
                        nameof(CandleData.High),
                        nameof(CandleData.Low),
                        nameof(CandleData.Close),
                        nameof(CandleData.Volume),

                        // padrões convertidos
                        "IsHammerF",
                        "IsInvertedHammerF",
                        "IsDojiF",
                        "IsBullishEngulfingF",
                        "IsBearishEngulfingF",
                        "IsShootingStarF",
                        "IsMorningStarF",
                        "IsEveningStarF",

                        // indicadores numéricos
                        nameof(CandleData.DistanciaParaResistencia),
                        nameof(CandleData.DistanciaParaSuporte),
                        nameof(CandleData.SMA),
                        nameof(CandleData.EMA),
                        nameof(CandleData.RSI),
                        nameof(CandleData.ATR),
                        nameof(CandleData.BollingerUpper),
                        nameof(CandleData.BollingerLower),

                        // flags convertidas
                        "RompeuResistenciaF",
                        "RompeuSuporteF"
                    ))
                    .Append(mlContext.Regression.Trainers.FastTree(
                        labelColumnName: nameof(CandleData.CloseNext),
                        featureColumnName: "Features"
                    ));

                var model = pipeline.Fit(trainDv);
                var predDv = model.Transform(testDv);
                var metrics = mlContext.Regression.Evaluate(predDv, labelColumnName: nameof(CandleData.CloseNext));

                rmses.Add(metrics.RootMeanSquaredError);
                r2s.Add(metrics.RSquared);
            }

            AnsiConsole.MarkupLine($"[green]Walk‑Forward RMSE médio:[/] {rmses.Average():F4}");
            AnsiConsole.MarkupLine($"[green]Walk‑Forward R² médio:[/]   {r2s.Average():P2}");
        }
    }
}
