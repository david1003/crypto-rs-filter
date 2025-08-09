using Newtonsoft.Json;
using RSDailyFilter.Enums;

namespace RSDailyFilter.ExchangeService;

/// <summary>
/// 幣安
/// </summary>
public class BinanceExchangeService(HttpClient httpClient) : IExchangeService
{
    public ExchangeServiceEnum ServiceType => ExchangeServiceEnum.Binance;
    
    public Task InitAsync()
    {
        return Task.CompletedTask;
    }

    public async Task<List<string>> GetTradingPairs()
    {
        string response = await httpClient.GetStringAsync("https://fapi.binance.com/fapi/v1/exchangeInfo");
        var data = JsonConvert.DeserializeObject<ExchangeInfo>(response);

        return data.Symbols
            .Where(s => s is { QuoteAsset: "USDT", ContractType: "PERPETUAL", Status: "TRADING" })
            .Select(s => s.Symbol)
            .ToList() ?? [];
    }

    public async Task<List<double>> GetHistoricalPrices(string symbol, int interval, PriceIntervalLetterEnum letterEnum, int limit)
    {
        string url = $"https://fapi.binance.com/fapi/v1/klines?symbol={symbol}&interval={interval}{letterEnum.ToString().ToLower()}&limit={limit}";
        string response = await httpClient.GetStringAsync(url);

        var data = JsonConvert.DeserializeObject<List<List<object>>>((response));
        return data?.Select(k => Convert.ToDouble(k[4])).ToList() ?? []; // 收盤價
    }

    public string ConvertSymbolsToTradingViewFormat(List<string> symbols)
    {
        return string.Join(",", symbols.Select(s => "BINANCE:" + s + ".P"));
    }

    struct ExchangeInfo
    {
        public List<SymbolInfo> Symbols { get; set; }
    }
    
    struct SymbolInfo
    {
        public string Symbol { get; set; }
        public string QuoteAsset { get; set; }
        public string ContractType { get; set; }
        public string Status { get; set; }
    }
}