using TradeIA.Models;

namespace TradeIA.Utils;

public static class AgrupadorDeCandles
{
    public static List<CandleData> Agrupar(List<CandleData> candles, int minutos)
    {
        var agrupados = new List<CandleData>();

        for (int i = 0; i < candles.Count; i += minutos)
        {
            var grupo = candles.Skip(i).Take(minutos).ToList();
            if (grupo.Count < minutos)
                break;

            var candle = new CandleData
            {
                Open = grupo.First().Open,
                Close = grupo.Last().Close,
                High = grupo.Max(c => c.High),
                Low = grupo.Min(c => c.Low),
                Volume = grupo.Sum(c => c.Volume),
                CloseNext = grupo.Last().Close // Placeholder (vai ser atualizado depois)
            };

            agrupados.Add(candle);
        }

        // Atualiza CloseNext e Resultado
        for (int i = 0; i < agrupados.Count - 1; i++)
        {
            agrupados[i].CloseNext = agrupados[i + 1].Close;
            agrupados[i].Resultado = agrupados[i].CloseNext > agrupados[i].Close ? "UP" : "DOWN";
        }

        // Remove o último porque não tem o próximo
        agrupados.RemoveAt(agrupados.Count - 1);

        return agrupados;
    }
}
