using System.Globalization;
using Spectre.Console;
using TradeIA.ML;
using TradeIA.Models;
using TradeIA.Utils;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("         IA Trade v1.0.0\n");

var candles = new List<CandleData>();

Console.Write("Arquivo Base: ");
string? file = Console.ReadLine();
if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
{
    Console.WriteLine("Arquivo não encontrado!");
    return;
}

var linhas = File.ReadAllLines(file).Skip(1).ToArray();

for (int i = 0; i < linhas.Length - 1; i++){
    var atual = linhas[i].Split(',');
    var proximo = linhas[i + 1].Split(',');

    try
    {
        var candle = new CandleData
        {
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

// Avaliar os modelos de forma isolada
var agrupados1m = AgrupadorDeCandles.Agrupar(candles, 1);
var agrupados5m = AgrupadorDeCandles.Agrupar(candles, 5);
var agrupados10m = AgrupadorDeCandles.Agrupar(candles, 10);
var agrupados15m = AgrupadorDeCandles.Agrupar(candles, 15);
var agrupados30m = AgrupadorDeCandles.Agrupar(candles, 30);
var agrupados1h = AgrupadorDeCandles.Agrupar(candles, 60);

var avaliador = new Avaliador();

Console.WriteLine();
AnsiConsole.MarkupLine("[yellow]🔎 Avaliando candles de 1 minutos...[/]");
avaliador.AvaliarModelos(agrupados1m);

Console.WriteLine();
AnsiConsole.MarkupLine("[yellow]🔎 Avaliando candles de 5 minutos...[/]");
avaliador.AvaliarModelos(agrupados5m);

Console.WriteLine();
AnsiConsole.MarkupLine("[yellow]🔎 Avaliando candles de 10 minutos...[/]");
avaliador.AvaliarModelos(agrupados10m);

Console.WriteLine();
AnsiConsole.MarkupLine("[yellow]🔎 Avaliando candles de 15 minutos...[/]");
avaliador.AvaliarModelos(agrupados15m);

Console.WriteLine();
AnsiConsole.MarkupLine("[yellow]🔎 Avaliando candles de 30 minutos...[/]");
avaliador.AvaliarModelos(agrupados30m);

Console.WriteLine();
AnsiConsole.MarkupLine("[yellow]🔎 Avaliando candles de 1 hora...[/]");
avaliador.AvaliarModelos(agrupados1h);
Console.ReadLine();