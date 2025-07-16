using System.Globalization;
using TradeIA.Models;

namespace TradeIA.Utils;

public static class CsvLoader
{
    public static List<CandleData> Load(string path)
    {
        var candles = new List<CandleData>();
        var lines = File.ReadAllLines(path).Skip(1).ToArray();

        for (int i = 0; i < lines.Length - 1; i++)
        {
            var atual = lines[i].Split(',');
            var proximo = lines[i + 1].Split(',');

            try
            {
                var candle = new CandleData
                {
                    Time = DateTime.Parse(atual[0], CultureInfo.InvariantCulture),
                    Open = float.Parse(atual[1], CultureInfo.InvariantCulture),
                    High = float.Parse(atual[2], CultureInfo.InvariantCulture),
                    Low = float.Parse(atual[3], CultureInfo.InvariantCulture),
                    Close = float.Parse(atual[4], CultureInfo.InvariantCulture),
                    Volume = float.Parse(atual[5], CultureInfo.InvariantCulture),
                    CloseNext = float.Parse(proximo[4], CultureInfo.InvariantCulture)
                };

                candle.Resultado = candle.CloseNext > candle.Close ? "UP" : "DOWN";
                candles.Add(candle);
            }
            catch
            {
                Console.WriteLine("⚠️ Erro ao processar linha: " + i);
            }
        }

        return candles;
    }
}
