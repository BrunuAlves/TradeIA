
namespace TradeIA.Models;

public class CandleData
{
    //Dados
    public float Open { get; set; }
    public float High { get; set; }
    public float Low { get; set; }
    public float Close { get; set; }
    public float Volume { get; set; }

    //Previsao
    public float CloseNext { get; set; }
    public string? Resultado { get; set; }

    //Tecnicas
    public bool IsHammer { get; set; }
    public bool IsInvertedHammer { get; set; }
    public bool IsDoji { get; set; }
    public bool IsBullishEngulfing { get; set; }
    public bool IsBearishEngulfing { get; set; }
    public bool IsShootingStar { get; set; }
    public bool IsMorningStar { get; set; }
    public bool IsEveningStar { get; set; }

    // Suporte/Resistência
    public float DistanciaParaResistencia { get; set; }
    public float DistanciaParaSuporte { get; set; }
    public bool RompeuResistencia { get; set; }
    public bool RompeuSuporte { get; set; }

    // Médias Móveis & RSI
    public float SMA { get; set; }
    public float EMA { get; set; }
    public float RSI { get; set; }

    //  ATR & Bollinger
    public float ATR { get; set; }
    public float BollingerUpper { get; set; }
    public float BollingerLower { get; set; }


}
