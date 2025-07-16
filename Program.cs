using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Spectre.Console;
using TradeIA.ML;
using TradeIA.Models;
using TradeIA.Utils;

namespace TradeIA
{
    class Program
    {
        static void Main(string[] args)
        {
            AnsiConsole.MarkupLine("[bold underline]=== TradeIA 🕯️ ML Analysis ===[/]\n");

            // Escolhe modo de operação
            AnsiConsole.MarkupLine("[yellow]Selecione o modo:[/]");
            AnsiConsole.MarkupLine("[green][1][/]: Treinar modelos");
            AnsiConsole.MarkupLine("[green][2][/]: Prever único candle");
            AnsiConsole.MarkupLine("[green][3][/]: Backtest de lookbacks");
            var modo = Console.ReadLine()?.Trim();

            // Importa CSV e prepara dados
            var file = AskCsvFile();
            var candles = LoadCandles(file);
            ComputeNextAndResult(candles);
            DetectPatterns(candles);
            ComputeIndicators(candles);

            // Treina ou carrega engines
            bool retrain = modo == "1";
            var regEngine = new Regressao(retrain).Treinar(candles);
            var classEngine = new Classificacao(retrain).Treinar(candles);

            switch (modo)
            {
                case "1": // Treinar + avaliar
                    AskAndEvaluate(candles, regEngine, classEngine, retrain);
                    break;
                case "2": // Prever último candle
                    var last = candles.Last();
                    var pr = regEngine.Predict(last);
                    var pc = classEngine.Predict(last);
                    AnsiConsole.MarkupLine($"[blue]Predição (Regressão):[/] {pr.PredictedCloseNext:0.0000}");
                    AnsiConsole.MarkupLine($"[blue]Predição (Classificação):[/] {pc.Resultado}");
                    break;
                case "3": // Backtest lookbacks
                    var periods = new Dictionary<string, TimeSpan>
                    {
                        { "6M", TimeSpan.FromDays(180) },
                        { "3M", TimeSpan.FromDays(90)  },
                        { "1M", TimeSpan.FromDays(30)  },
                        { "1W", TimeSpan.FromDays(7)   },
                        { "1D", TimeSpan.FromDays(1)   }
                    };
                    var tester = new Backtester();
                    tester.EvaluateLookbacks(
                        allCandles: candles,
                        referenceDate: DateTime.Today,
                        periods: periods,
                        regEngine: regEngine,
                        classEngine: classEngine);
                    break;
                default:
                    AnsiConsole.MarkupLine("[red]Modo inválido.[/]");
                    break;
            }

            AnsiConsole.MarkupLine("\n[green]Processo concluído.[/]");
        }

        // Métodos auxiliares:
        static string AskCsvFile()
        {
            while (true)
            {
                Console.Write("Arquivo Base (CSV): ");
                var f = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(f) && File.Exists(f)) return f;
                AnsiConsole.MarkupLine("[red]Caminho inválido. Tente novamente.[/]");
            }
        }
        static List<CandleData> LoadCandles(string file)
        {
            return File.ReadAllLines(file).Skip(1)
                .Select(line => line.Split(','))
                .Where(cols => cols.Length >= 6)
                .Select(cols => new CandleData
                {
                    Open = float.Parse(cols[1], CultureInfo.InvariantCulture),
                    High = float.Parse(cols[2], CultureInfo.InvariantCulture),
                    Low = float.Parse(cols[3], CultureInfo.InvariantCulture),
                    Close = float.Parse(cols[4], CultureInfo.InvariantCulture),
                    Volume = float.Parse(cols[5], CultureInfo.InvariantCulture)
                })
                .ToList();
        }
        static void ComputeNextAndResult(List<CandleData> c)
        {
            for (int i = 0; i < c.Count - 1; i++)
            {
                c[i].CloseNext = c[i + 1].Close;
                c[i].Resultado = c[i + 1].Close > c[i].Close ? "UP" : "DOWN";
            }
            c.RemoveAt(c.Count - 1);
        }
        static void DetectPatterns(List<CandleData> c)
        {
            for (int i = 0; i < c.Count; i++)
            {
                var x = c[i];
                x.IsHammer = TecnicasCandle.IsHammer(x);
                x.IsInvertedHammer = TecnicasCandle.IsInvertedHammer(x);
                x.IsDoji = TecnicasCandle.IsDoji(x);
                x.IsShootingStar = TecnicasCandle.IsShootingStar(x);
                if (i >= 1)
                {
                    x.IsBullishEngulfing = TecnicasCandle.IsBullishEngulfing(c[i - 1], x);
                    x.IsBearishEngulfing = TecnicasCandle.IsBearishEngulfing(c[i - 1], x);
                }
                if (i >= 2)
                {
                    x.IsMorningStar = TecnicasCandle.IsMorningStar(c, i);
                    x.IsEveningStar = TecnicasCandle.IsEveningStar(c, i);
                }
            }
        }
        static void ComputeIndicators(List<CandleData> c)
        {
            IndicadoresCandle.PreencherSuporteResistencia(c, 5, 5);
            IndicadoresCandle.PreencherSMA(c, 14);
            IndicadoresCandle.PreencherEMA(c, 14);
            IndicadoresCandle.PreencherRSI(c, 14);
            IndicadoresCandle.PreencherATR(c, 14);
            IndicadoresCandle.PreencherBollinger(c, 20, 2f);
        }
        static void AskAndEvaluate(List<CandleData> c,
                                   PredictionEngine<CandleData, CandleRegressionPrediction> re,
                                   PredictionEngine<CandleData, CandleClassificacaoPrediction> ce,
                                   bool retrain)
        {
            var tf = AnsiConsole.Prompt(
                new SelectionPrompt<int>()
                    .Title("Escolha timeframe para avaliação:")
                    .AddChoices(1, 5, 10, 15, 30, 60));
            var agrup = AgrupadorDeCandles.Agrupar(c, tf);
            var aval = new Avaliador(retrain);
            AnsiConsole.Write(new Rule($"📊 Avaliação {tf}m").RuleStyle("yellow").Centered());
            aval.AvaliarModelos(agrup);
            aval.WalkForwardValidation(agrup, (int)(agrup.Count * 0.8), agrup.Count - (int)(agrup.Count * 0.8));

            // backtest simples no mesmo timeframe
            var bt = new Backtester();
            decimal final = bt.Run(agrup, re, ce);
            AnsiConsole.MarkupLine($"[green]Backtest final {tf}m → saldo: {final:C}[/]");
        }
    }
}
