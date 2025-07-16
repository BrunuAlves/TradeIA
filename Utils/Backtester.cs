using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using TradeIA.Models;

namespace TradeIA.Utils;

/// <summary>
/// Simple backtesting utilities for CandleData predictions.
/// </summary>
public class Backtester
{
    /// <summary>
    /// Runs a basic long/short backtest using both regression and classification predictions.
    /// If both models agree on the direction, a trade is opened for the next bar.
    /// </summary>
    public decimal Run(
        IList<CandleData> candles,
        PredictionEngine<CandleData, CandleRegressionPrediction> reg,
        PredictionEngine<CandleData, CandleClassificacaoPrediction> cls)
    {
        decimal balance = 0m;
        for (int i = 0; i < candles.Count - 1; i++)
        {
            var cur = candles[i];
            var nextClose = candles[i + 1].Close;
            bool upReg = reg.Predict(cur).PredictedCloseNext > cur.Close;
            bool upCls = cls.Predict(cur).Resultado == "UP";

            if (upReg && upCls)
                balance += (decimal)(nextClose - cur.Close);
            else if (!upReg && !upCls)
                balance += (decimal)(cur.Close - nextClose);
        }
        return balance;
    }

    /// <summary>
    /// Evaluates different lookback windows and prints the result for each period.
    /// </summary>
    public void EvaluateLookbacks(
        IList<CandleData> allCandles,
        DateTime referenceDate,
        IDictionary<string, TimeSpan> periods,
        PredictionEngine<CandleData, CandleRegressionPrediction> regEngine,
        PredictionEngine<CandleData, CandleClassificacaoPrediction> classEngine)
    {
        int total = allCandles.Count;
        double maxDays = periods.Values.Max(p => p.TotalDays);

        foreach (var kv in periods)
        {
            int take = (int)(total * (kv.Value.TotalDays / maxDays));
            if (take < 1) take = 1;
            var subset = allCandles.Skip(total - take).ToList();
            decimal result = Run(subset, regEngine, classEngine);
            Spectre.Console.AnsiConsole.MarkupLine($"[green]{kv.Key}[/]: {result:C}");
        }
    }
}
