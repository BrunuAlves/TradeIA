using System;
using System.Collections.Generic;
using System.Linq;
using TradeIA.Models;

namespace TradeIA.Utils
{
    public static class IndicadoresCandle
    {
        // ─── A) Suporte & Resistência ─────────────────────────────────────────

        // Detecta pivôs altos (resistências) e baixos (suportes)
        public static List<int> DetectarPivosAltos(List<CandleData> c, int lookback = 5)
        {
            var pivots = new List<int>();
            for (int i = lookback; i < c.Count - lookback; i++)
            {
                if (Enumerable.Range(i - lookback, lookback * 2 + 1)
                    .All(j => c[j].High <= c[i].High))
                    pivots.Add(i);
            }
            return pivots;
        }
        public static List<int> DetectarPivosBaixos(List<CandleData> c, int lookback = 5)
        {
            var pivots = new List<int>();
            for (int i = lookback; i < c.Count - lookback; i++)
            {
                if (Enumerable.Range(i - lookback, lookback * 2 + 1)
                    .All(j => c[j].Low >= c[i].Low))
                    pivots.Add(i);
            }
            return pivots;
        }

        // Ajusta uma linha por regressão linear simples
        public static (float slope, float intercept) RegressaoLinear(IEnumerable<(float x, float y)> pontos)
        {
            var pts = pontos.ToList();
            int n = pts.Count;
            float mx = pts.Average(p => p.x);
            float my = pts.Average(p => p.y);
            float num = pts.Sum(p => (p.x - mx) * (p.y - my));
            float den = pts.Sum(p => (p.x - mx) * (p.x - mx));
            float m = den == 0 ? 0 : num / den;
            float b = my - m * mx;
            return (m, b);
        }

        // Para cada candle, calcula distância % até linha de resistência e suporte
        public static void PreencherSuporteResistencia(List<CandleData> candles, int lookback = 5, int pivotsCount = 5)
        {
            var altos = DetectarPivosAltos(candles, lookback).TakeLast(pivotsCount).ToList();
            var baixos = DetectarPivosBaixos(candles, lookback).TakeLast(pivotsCount).ToList();

            var ptsAltos = altos.Select(i => ((float)i, candles[i].High));
            var ptsBaixos = baixos.Select(i => ((float)i, candles[i].Low));

            var (mR, bR) = RegressaoLinear(ptsAltos);
            var (mS, bS) = RegressaoLinear(ptsBaixos);

            for (int i = 0; i < candles.Count; i++)
            {
                float res = mR * i + bR;
                float sup = mS * i + bS;
                candles[i].DistanciaParaResistencia = (res - candles[i].Close) / candles[i].Close;
                candles[i].DistanciaParaSuporte = (candles[i].Close - sup) / candles[i].Close;
                candles[i].RompeuResistencia = candles[i].High > res;
                candles[i].RompeuSuporte = candles[i].Low < sup;
            }
        }

        // ─── B) Médias Móveis & RSI ────────────────────────────────────────────

        // SMA de período N
        public static void PreencherSMA(List<CandleData> candles, int period = 14)
        {
            for (int i = 0; i < candles.Count; i++)
            {
                if (i + 1 >= period)
                {
                    candles[i].SMA = candles.Skip(i + 1 - period).Take(period).Average(c => c.Close);
                }
                else candles[i].SMA = candles[i].Close;
            }
        }

        // EMA de período N
        public static void PreencherEMA(List<CandleData> candles, int period = 14)
        {
            float k = 2f / (period + 1);
            float prev = candles[0].Close;
            candles[0].EMA = prev;
            for (int i = 1; i < candles.Count; i++)
            {
                float ema = candles[i].Close * k + prev * (1 - k);
                candles[i].EMA = ema;
                prev = ema;
            }
        }

        // RSI de período N
        public static void PreencherRSI(List<CandleData> candles, int period = 14)
        {
            float gain = 0, loss = 0;
            for (int i = 1; i < candles.Count; i++)
            {
                float diff = candles[i].Close - candles[i - 1].Close;
                gain += Math.Max(diff, 0);
                loss += Math.Max(-diff, 0);

                if (i >= period)
                {
                    float avgGain = gain / period;
                    float avgLoss = loss / period;
                    candles[i].RSI = avgLoss == 0 ? 100 : 100 - (100 / (1 + avgGain / avgLoss));

                    // desliza janela
                    float prevDiff = candles[i - period + 1].Close - candles[i - period].Close;
                    gain -= Math.Max(prevDiff, 0);
                    loss -= Math.Max(-prevDiff, 0);
                }
                else candles[i].RSI = 50;
            }
        }

        // ─── C) ATR & Bollinger Bands ───────────────────────────────────────────

        // True Range
        public static float TrueRange(CandleData cur, CandleData prev)
        {
            return Math.Max(
                cur.High - cur.Low,
                Math.Max(Math.Abs(cur.High - prev.Close), Math.Abs(cur.Low - prev.Close))
            );
        }

        // ATR período N
        public static void PreencherATR(List<CandleData> candles, int period = 14)
        {
            float trSum = 0;
            for (int i = 1; i < candles.Count; i++)
            {
                float tr = TrueRange(candles[i], candles[i - 1]);
                trSum += tr;
                if (i == period)
                    candles[i].ATR = trSum / period;
                else if (i > period)
                {
                    candles[i].ATR = (candles[i - 1].ATR * (period - 1) + tr) / period;
                }
                else
                    candles[i].ATR = tr;
            }
        }

        // Bollinger Bands: SMA ± k * stdDev
        public static void PreencherBollinger(List<CandleData> candles, int period = 20, float k = 2f)
        {
            for (int i = 0; i < candles.Count; i++)
            {
                if (i + 1 >= period)
                {
                    var slice = candles.Skip(i + 1 - period).Take(period).Select(c => c.Close).ToArray();
                    float sma = slice.Average();
                    float std = (float)Math.Sqrt(slice.Select(v => (v - sma) * (v - sma)).Average());
                    candles[i].BollingerUpper = sma + k * std;
                    candles[i].BollingerLower = sma - k * std;
                }
                else
                {
                    candles[i].BollingerUpper = candles[i].Close;
                    candles[i].BollingerLower = candles[i].Close;
                }
            }
        }
    }
}
