using System.Globalization;
using Spectre.Console;
using TradeIA.ML;
using TradeIA.Models;
using TradeIA.Utils;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("         IA Trade v1.0.0\n");

Console.Write("Arquivo Base: ");
string? file = Console.ReadLine();
if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
{
    Console.WriteLine("Arquivo não encontrado!");
    return;
}

Console.WriteLine();
AnsiConsole.MarkupLine("[yellow]Selecione o modo:[/]");
Console.WriteLine("1 - Avaliar histórico");
Console.WriteLine("2 - Monitorar sinais");
Console.Write("Opção: ");
var opcao = Console.ReadLine();

switch (opcao)
{
    case "1":
        Avaliar(file);
        break;
    case "2":
        Sinal.Monitorar(file);
        break;
    default:
        Console.WriteLine("Opção inválida!");
        break;
}

void Avaliar(string caminho)
{
    var candles = CsvLoader.Load(caminho);

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
}
