using System;
using MtApiService;
using MTApiService.MT5;

namespace TradeIA.Utils
{
    /// <summary>
    /// Classe inicial para conectar em conta MetaTrader 5 usando MTApiService.
    /// Utilizada como base para backtests em conta real ou trading ao vivo.
    /// </summary>
    public class RealBacktester
    {
        private readonly MtApiClient _client;

        /// <summary>
        /// Constrói o conector MetaTrader usando credenciais.
        /// </summary>
        /// <param name="server">Nome ou IP do servidor (ex: "MetaQuotes-Demo").</param>
        /// <param name="login">Número de login da conta.</param>
        /// <param name="password">Senha de acesso.</param>
        /// <param name="investorPassword">Senha de investidor (leitura).</param>
        public RealBacktester(string server, int login, string password, string investorPassword)
        {
            _client = new MtApiClient();
            _client.BeginConnect(server, login, password, investorPassword);

            if (!_client.IsConnected)
            {
                throw new InvalidOperationException("Falha ao conectar no MetaTrader 5. Verifique as credenciais e a conexão.");
            }
        }

        /// <summary>
        /// Verifica se o cliente está conectado.
        /// </summary>
        public bool IsConnected => _client.IsConnected;

        /// <summary>
        /// Fecha a conexão com o servidor MetaTrader.
        /// </summary>
        public void Disconnect()
        {
            if (_client.IsConnected)
            {
                _client.BeginDisconnect();
            }
        }
    }
}
