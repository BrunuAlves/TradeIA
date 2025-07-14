
namespace TradeIA.Models;

public class CandleData
{
    public float Open { get; set; }
    public float High { get; set; }
    public float Low { get; set; }
    public float Close { get; set; }
    public float Volume { get; set; }

    public float CloseNext { get; set; }
    public string? Resultado { get; set; }
}
