using TradeIA.Models;
using Spectre.Console;

namespace TradeIA.ML;

public class Avaliador
{
    private readonly Regressao _regressao = new();
    private readonly Classificacao _classificacao = new();

    public void AvaliarModelos(List<CandleData> candles)
    {
        int total = candles.Count;
        int acertosReg = 0;
        int acertosClass = 0;
        int ambosConcordam = 0;
        int concordamAcertaram = 0;

        var engineReg = _regressao.Treinar(candles);
        var engineClass = _classificacao.Treinar(candles);

        foreach (var candle in candles)
        {
            var predReg = engineReg.Predict(candle);
            var predClass = engineClass.Predict(candle);

            string direcaoReg = predReg.PredictedCloseNext > candle.Close ? "UP" : "DOWN";
            string direcaoClass = predClass.Resultado;
            string direcaoReal = candle.Resultado;

            if (direcaoReg == direcaoReal)
                acertosReg++;

            if (direcaoClass == direcaoReal)
                acertosClass++;

            if (direcaoReg == direcaoClass)
            {
                ambosConcordam++;
                if (direcaoReg == direcaoReal)
                    concordamAcertaram++;
            }
        }

        double pctReg = acertosReg * 100.0 / total;
        double pctClass = acertosClass * 100.0 / total;
        double pctConcordam = ambosConcordam * 100.0 / total;
        double pctConcordamCorretos = ambosConcordam > 0 ? concordamAcertaram * 100.0 / ambosConcordam : 0;

        AnsiConsole.Write(new Rule("📊 AVALIAÇÃO FINAL").RuleStyle("yellow").Centered());
        AnsiConsole.MarkupLine($"[green]Total de candles:[/] {total}");

        var chart = new BarChart()
            .Width(60)
            .Label("Precisão dos modelos (%)")
            .CenterLabel();

        chart.AddItem("Regressão", (float)pctReg, ConsoleColor.Blue);
        chart.AddItem("Classificação", (float)pctClass, ConsoleColor.Green);
        chart.AddItem("Ambos Concordam", (float)pctConcordam, ConsoleColor.Yellow);
        chart.AddItem("Acerto nas Concordâncias", (float)pctConcordamCorretos, ConsoleColor.Magenta);

        AnsiConsole.Write(chart);
    }

}
