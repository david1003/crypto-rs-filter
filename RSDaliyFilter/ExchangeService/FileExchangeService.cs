using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RSDailyFilter.Common;
using RSDailyFilter.Enums;

namespace RSDailyFilter.ExchangeService
{
    /// <summary>
    /// 由檔案讀取之前已存檔的結果
    /// </summary>
    public class FileExchangeService(string basePath, DateTime targetDate, string symbolPriceFileName) : IExchangeService
    {
        private readonly List<SymbolPrice> _symbolPrices = [];

        public ExchangeServiceEnum ServiceType => ExchangeServiceEnum.File;

        public async Task InitAsync()
        {
            //直接讀取SymbolPrice檔案就好
            string dailyResultFolderPath = Path.Combine(basePath, $"{targetDate.ToString(SystemConstants.FolderDateFormat)}");
            if (Directory.Exists(dailyResultFolderPath))
            {
                string symbolPriceFileContent =
                    await Tools.ReadFileContent(dailyResultFolderPath, symbolPriceFileName);

                var lines = symbolPriceFileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length != 2) continue; // 格式錯誤略過

                    var symbol = parts[0].Trim();
                    var prices = parts[1]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => double.TryParse(p, out var val) ? val : 0.0)
                        .ToList();

                    _symbolPrices.Add(new SymbolPrice(symbol, prices));
                }
            }
        }
        
        public Task<List<string>> GetTradingPairs()
        {
            return Task.FromResult(_symbolPrices.Select(s => s.Symbol).ToList());
        }

        public Task<List<double>> GetHistoricalPrices(string symbol, int interval, PriceIntervalLetterEnum letterEnum, int limit)
        {
            return Task.FromResult(_symbolPrices.FirstOrDefault(s => s.Symbol == symbol)?.Prices ?? []);
        }

        public string ConvertSymbolsToTradingViewFormat(List<string> symbols)
        {
            return string.Join(",", symbols.Select(s => "BINANCE:" + s + ".P"));
        }

    }

    class SymbolPrice(string symbol, List<double> prices)
    {
        public string Symbol { get; set; } = symbol;
        public List<double> Prices { get; set; } = prices;
    }
}
