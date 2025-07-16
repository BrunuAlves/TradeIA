using System;
using System.Collections.Generic;
using Spectre.Console;
using TradeIA.Models;

namespace TradeIA.Utils
{
    public static class TecnicasCandle
    {

        /// <summary>
        /// Gera todas as combinações de técnicas de tamanho entre min e max,
        /// conta quantas vezes aparecem e qual a taxa de acerto UP/DOWN,
        /// e exibe tabela + gráfico dos top padrões.
        /// </summary>
        public static void AvaliarTecnicas(List<CandleData> candles, int minOcorrencias = 10, int tamanhoMin = 1, int tamanhoMax = int.MaxValue)
        {
            // Mapeia nome → função que detecta a técnica naquele candle
            var tecnicas = new Dictionary<string, Func<CandleData, bool>>
            {
                { "Hammer",       c => c.IsHammer },
                { "Inv.Hammer",   c => c.IsInvertedHammer },
                { "Doji",         c => c.IsDoji },
                { "BullEngulf",   c => c.IsBullishEngulfing },
                { "BearEngulf",   c => c.IsBearishEngulfing },
                { "ShootingStar", c => c.IsShootingStar },
                { "MorningStar",  c => c.IsMorningStar },
                { "EveningStar",  c => c.IsEveningStar }
            };

            // Gera todas as combinações de 1 até N técnicas
            var todasComb = new List<List<string>>();
            var nomes = tecnicas.Keys.ToList();
            for (int tam = 1; tam <= nomes.Count; tam++)
                todasComb.AddRange(Combinar(nomes, tam));

            var resultados = new List<(string Combo, int Total, int Acertos, double Taxa)>();

            // Para cada combo, conta as vezes que todas as técnicas estão presentes
            foreach (var combo in todasComb)
            {
                int total = 0, acertos = 0;
                foreach (var c in candles)
                {
                    // só conta candle onde todas as técnicas do combo retornam true
                    if (combo.All(t => tecnicas[t](c)))
                    {
                        total++;
                        if (c.Resultado == "UP")
                            acertos++;
                    }
                }

                if (total >= minOcorrencias)
                {
                    resultados.Add((
                        Combo: string.Join("+", combo),
                        Total: total,
                        Acertos: acertos,
                        Taxa: acertos * 100.0 / total
                    ));
                }
            }

            if (resultados.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Nenhum padrão com ocorrências suficientes para análise.[/]");
                return;
            }

            // Pega os top 10 por taxa de acerto
            var top = resultados
                .OrderByDescending(r => r.Taxa)
                .Take(10)
                .ToList();

            // Exibe tabela
            AnsiConsole.Write(new Rule("📍 Melhores Padrões").RuleStyle("green").Centered());
            var table = new Table().Border(TableBorder.Rounded).AddColumns("Padrão", "Ocorrências", "Acertos", "Taxa (%)");
            foreach (var r in top)
                table.AddRow(r.Combo, r.Total.ToString(), r.Acertos.ToString(), $"{r.Taxa:0.00}%");
            AnsiConsole.Write(table);

            // Exibe gráfico de barras
            if (top.Count > 0)
            {
                var chart = new BarChart()
                    .Label("Taxa de Acerto (%)")
                    .Width(60)
                    .CenterLabel();

                foreach (var r in top)
                    chart.AddItem(r.Combo, (float)r.Taxa, ConsoleColor.Cyan);

                AnsiConsole.Write(chart);
            }
        }

        // Gera combinações de lista de strings tomando 'tamanho' por vez
        private static List<List<string>> Combinar(List<string> lista, int tamanho)
        {
            if (tamanho == 0) return new List<List<string>> { new List<string>() };
            if (tamanho > lista.Count) return new List<List<string>>();

            return lista.SelectMany((item, idx) =>
                Combinar(lista.Skip(idx + 1).ToList(), tamanho - 1)
                    .Select(rest => {
                        var combo = new List<string> { item };
                        combo.AddRange(rest);
                        return combo;
                    })
            ).ToList();
        }

        public static bool IsHammer(CandleData c)
        {
            var body = Math.Abs(c.Open - c.Close);
            var lowerWick = Math.Min(c.Open, c.Close) - c.Low;
            var upperWick = c.High - Math.Max(c.Open, c.Close);

            // Mais permissivo: 1.2x em vez de 2x, e upperWick < 0.8 * body
            return body > 0 &&
                   lowerWick > body * 1.2 &&
                   upperWick < body * 0.8;
        }

        public static bool IsInvertedHammer(CandleData c)
        {
            var body = Math.Abs(c.Open - c.Close);
            var lowerWick = Math.Min(c.Open, c.Close) - c.Low;
            var upperWick = c.High - Math.Max(c.Open, c.Close);

            return body > 0 &&
                   upperWick > body * 1.2 &&
                   lowerWick < body * 0.8;
        }

        public static bool IsDoji(CandleData c)
        {
            var body = Math.Abs(c.Open - c.Close);
            var range = c.High - c.Low;
            // Mais permissivo: até 20% do range
            return range > 0 && (body / range) < 0.2;
        }

        public static bool IsBullishEngulfing(CandleData ant, CandleData cur)
        {
            // Mesma lógica, pois engulfing é mais sobre relação de cores
            return ant.Close < ant.Open &&
                   cur.Close > cur.Open &&
                   cur.Open < ant.Close &&
                   cur.Close > ant.Open;
        }

        public static bool IsBearishEngulfing(CandleData ant, CandleData cur)
        {
            return ant.Close > ant.Open &&
                   cur.Close < cur.Open &&
                   cur.Open > ant.Close &&
                   cur.Close < ant.Open;
        }

        public static bool IsShootingStar(CandleData c)
        {
            var body = Math.Abs(c.Open - c.Close);
            var upperWick = c.High - Math.Max(c.Open, c.Close);
            var lowerWick = Math.Min(c.Open, c.Close) - c.Low;

            return body > 0 &&
                   upperWick > body * 1.2 &&
                   lowerWick < body * 0.8;
        }

        public static bool IsMorningStar(IList<CandleData> candles, int idx)
        {
            if (idx < 2 || idx >= candles.Count)
                return false;

            var a = candles[idx - 2];
            var b = candles[idx - 1];
            var c = candles[idx];

            // Padrão mais “macio”: admitimos candle intermediário até 50% como doji
            bool isDojiLike = Math.Abs(b.Open - b.Close) / (b.High - b.Low + 1e-4f) < 0.5f;

            return a.Close < a.Open &&
                   isDojiLike &&
                   c.Close > c.Open && c.Close > a.Open;
        }

        public static bool IsEveningStar(IList<CandleData> candles, int idx)
        {
            if (idx < 2 || idx >= candles.Count)
                return false;

            var a = candles[idx - 2];
            var b = candles[idx - 1];
            var c = candles[idx];

            bool isDojiLike = Math.Abs(b.Open - b.Close) / (b.High - b.Low + 1e-4f) < 0.5f;

            return a.Close > a.Open &&
                   isDojiLike &&
                   c.Close < c.Open && c.Close < a.Open;
        }
    }
}
