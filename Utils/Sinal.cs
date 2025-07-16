using System.Threading;
using TradeIA.Models;
using TradeIA.ML;

namespace TradeIA.Utils;

public static class Sinal
{
    public static void Monitorar(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            Console.WriteLine("Arquivo de dados não encontrado!");
            return;
        }

        int linhasLidas = 0;
        Console.WriteLine("Monitorando sinais... (Ctrl+C para sair)");

        while (true)
        {
            var linhas = File.ReadAllLines(csvPath).Skip(1).ToArray();
            if (linhas.Length <= linhasLidas)
            {
                Thread.Sleep(TimeSpan.FromSeconds(30));
                continue;
            }

            linhasLidas = linhas.Length;
            var candles = CsvLoader.Load(csvPath);
            EmitirSinais(candles);
            Thread.Sleep(TimeSpan.FromMinutes(1));
        }
    }

    private static void EmitirSinais(List<CandleData> candles)
    {
        foreach (int minutos in new[] { 1, 5, 15 })
        {
            var agrupados = AgrupadorDeCandles.Agrupar(candles, minutos);
            if (agrupados.Count < 10)
                continue;

            var treino = agrupados.Take(agrupados.Count - 1).ToList();
            var atual = agrupados.Last();

            var reg = new Regressao();
            var classif = new Classificacao();
            var regEngine = reg.Treinar(treino);
            var classEngine = classif.Treinar(treino);

            var pr = regEngine.Predict(atual);
            var pc = classEngine.Predict(atual);
            var direcaoReg = pr.PredictedCloseNext > atual.Close ? "UP" : "DOWN";

            if (direcaoReg == pc.Resultado &&
                Math.Abs(pr.PredictedCloseNext - atual.Close) / atual.Close > 0.0001)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {minutos}m -> {direcaoReg} ({pr.PredictedCloseNext:0.0000})");
            }
        }
    }
}
