using System.Text;
using System.Text.Json;
using RSDailyFilter.Common;
using RSDailyFilter.ExchangeService;
using RSDailyFilter.Models;
using Microsoft.Extensions.Configuration;

namespace RSDailyFilter.Services
{
    /// <summary>
    /// RS 分析服務實作 - 包含合約比較、RS計算、五日漸強功能
    /// </summary>
    public class RsAnalysisService : IRsAnalysisService
    {
        private readonly IExchangeService _exchangeService;
        private readonly IConfiguration _configuration;

        public RsAnalysisService(IExchangeService exchangeService, IConfiguration configuration)
        {
            _exchangeService = exchangeService ?? throw new ArgumentNullException(nameof(exchangeService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// 比較當前合約符號是否與昨日不同，返回比較結果
        /// </summary>
        public async Task<string?> CompareContractSymbolsAsync(List<string> symbols, string dailyResultFolderPath)
        {
            ArgumentNullException.ThrowIfNull(symbols);
            ArgumentException.ThrowIfNullOrWhiteSpace(dailyResultFolderPath);

            var symbolsFileName = _configuration["AppSettings:SymbolPriceFileName"];
            ArgumentException.ThrowIfNullOrWhiteSpace(symbolsFileName);

            // 取得昨日資料夾路徑
            string yesterdayPath = Path.Combine(
                Path.GetDirectoryName(dailyResultFolderPath)!,
                DateTime.Now.AddDays(-1).ToString(SystemConstants.FolderDateFormat)
            );

            List<string>? yesterdaySymbols = null;
            if (Directory.Exists(yesterdayPath))
            {
                try
                {
                    string yesterdaySymbolFileContent = await Tools.ReadFileContent(yesterdayPath, symbolsFileName);
                    if (!string.IsNullOrEmpty(yesterdaySymbolFileContent))
                    {
                        yesterdaySymbols = yesterdaySymbolFileContent
                            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                            .Select(line => line.Split('|')[0])
                            .ToList();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"讀取昨日合約資料失敗: {ex.Message}");
                }
            }

            // 比較並準備訊息
            if (yesterdaySymbols != null && !symbols.SequenceEqual(yesterdaySymbols))
            {
                var newSymbols = symbols.Except(yesterdaySymbols).ToList();
                var removedSymbols = yesterdaySymbols.Except(symbols).ToList();

                StringBuilder message = new StringBuilder("永續合約變更通知:\n");
                
                if (newSymbols.Any())
                {
                    message.AppendLine($"新增合約 ({newSymbols.Count}): {string.Join(", ", newSymbols.Select(s => s.Replace("USDT", "")))}");
                }
                
                if (removedSymbols.Any())
                {
                    message.AppendLine($"移除合約 ({removedSymbols.Count}): {string.Join(", ", removedSymbols.Select(s => s.Replace("USDT", "")))}");
                }

                return message.ToString();
            }
            else
            {
                Console.WriteLine("合約無變更。");
                return null;
            }
        }

        /// <summary>
        /// 計算並取得 RS 排名的標的清單
        /// </summary>
        public async Task<List<SymbolStrength>> GetRsRankedSymbolsAsync(List<string> symbols, string dailyResultFolderPath, int topCount = 30)
        {
            ArgumentNullException.ThrowIfNull(symbols);
            ArgumentException.ThrowIfNullOrWhiteSpace(dailyResultFolderPath);

            // 從配置檔案讀取參數
            int currentTermDays = Convert.ToInt32(_configuration["AppSettings:CurrentTermDays"]);
            int shortDays = Convert.ToInt32(_configuration["AppSettings:ShortDays"]);
            int middleDays = Convert.ToInt32(_configuration["AppSettings:MiddleDays"]);
            int longDays = Convert.ToInt32(_configuration["AppSettings:LongDays"]);
            int maxDays = new[] { currentTermDays, shortDays, middleDays, longDays }.Max();
            
            double currentTermPercentage = Convert.ToDouble(_configuration["AppSettings:CurrentTermPercentage"]);
            double shortTermPercentage = Convert.ToDouble(_configuration["AppSettings:ShortTermPercentage"]);
            double middleTermPercentage = Convert.ToDouble(_configuration["AppSettings:MiddleTermPercentage"]);
            double longTermPercentage = Convert.ToDouble(_configuration["AppSettings:LongTermPercentage"]);

            var symbolStrengths = new List<SymbolStrength>();
            string? ignoreCryptoListString = _configuration["AppSettings:IgnoreCryptoList"];
            List<string> ignoreCryptoList = string.IsNullOrEmpty(ignoreCryptoListString)
                ? new List<string>()
                : ignoreCryptoListString.Split(',').Select(s => s.Trim()).ToList();

            var symbolPriceFileName = _configuration["AppSettings:SymbolPriceFileName"];
            ArgumentException.ThrowIfNullOrWhiteSpace(symbolPriceFileName);
            
            StringBuilder symbolPriceSb = new StringBuilder();

            foreach (string symbol in symbols)
            {
                try
                {
                    if (ignoreCryptoList.Contains(symbol))
                    {
                        Console.WriteLine($"忽略標的: {symbol}");
                        continue;
                    }

                    // 獲取足夠的歷史價格數據
                    var prices = await _exchangeService.GetHistoricalPrices(symbol, 1, PriceIntervalLetterEnum.D, maxDays);
                    
                    if (prices.Count < maxDays)
                    {
                        Console.WriteLine($"標的 {symbol} 歷史數據不足，跳過處理");
                        continue; // 確保有足夠的數據
                    }

                    // 儲存價格數據到字串建構器
                    symbolPriceSb.AppendLine($"{symbol}{SystemConstants.PriceSymbolSplitter}{string.Join(SystemConstants.Splitter, prices)}");

                    double currentPrice = prices.Last();
                    double maCurrentTerm = prices.TakeLast(currentTermDays).Average();
                    double maShort = prices.TakeLast(shortDays).Average();
                    double maMiddle = prices.TakeLast(middleDays).Average();
                    double maLong = prices.TakeLast(longDays).Average();

                    double regressionShort = CalculateRegressionSlope(prices.TakeLast(shortDays).ToList());
                    double regressionMiddle = CalculateRegressionSlope(prices.TakeLast(middleDays).ToList());
                    double regressionLong = CalculateRegressionSlope(prices.TakeLast(longDays).ToList());

                    // 計算RS值時防止除零錯誤
                    double currentTermRs = maCurrentTerm != 0 ? (currentPrice / maCurrentTerm) * 100 : 0;
                    double shortRs = maShort != 0 ? (regressionShort / maShort) * 100 : 0;
                    double middleRs = maMiddle != 0 ? (regressionMiddle / maMiddle) * 100 : 0;
                    double longRs = maLong != 0 ? (regressionLong / maLong) * 100 : 0;

                    // 檢查並修正非有限值
                    currentTermRs = double.IsFinite(currentTermRs) ? currentTermRs : 0;
                    shortRs = double.IsFinite(shortRs) ? shortRs : 0;
                    middleRs = double.IsFinite(middleRs) ? middleRs : 0;
                    longRs = double.IsFinite(longRs) ? longRs : 0;

                    symbolStrengths.Add(new SymbolStrength
                    {
                        Symbol = symbol,
                        CurrentTermRs = currentTermRs,
                        ShortRs = shortRs,
                        MiddleRs = middleRs,
                        LongRs = longRs
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"處理標的 {symbol} 時發生錯誤: {ex.Message}");
                }
            }

            // 將價格資料寫入檔案
            if (symbolPriceSb.Length > 0)
            {
                await Tools.WriteTextToFile(dailyResultFolderPath, symbolPriceFileName, symbolPriceSb.ToString());
            }

            if (!symbolStrengths.Any())
            {
                return new List<SymbolStrength>();
            }

            // 計算百分位排名
            var currentTermRanks = GetPercentRanks(symbolStrengths.Select(s => s.CurrentTermRs).ToList());
            var shortRanks = GetPercentRanks(symbolStrengths.Select(s => s.ShortRs).ToList());
            var middleRanks = GetPercentRanks(symbolStrengths.Select(s => s.MiddleRs).ToList());
            var longRanks = GetPercentRanks(symbolStrengths.Select(s => s.LongRs).ToList());

            // 設定排名並計算綜合強度
            for (int i = 0; i < symbolStrengths.Count; i++)
            {
                symbolStrengths[i].CurrentTermRsRank = currentTermRanks[i];
                symbolStrengths[i].ShortRsRank = shortRanks[i];
                symbolStrengths[i].MiddleRsRank = middleRanks[i];
                symbolStrengths[i].LongRsRank = longRanks[i];

                // 使用配置檔案中的權重計算加權綜合強度
                symbolStrengths[i].Strength = 
                    currentTermRanks[i] * currentTermPercentage + 
                    shortRanks[i] * shortTermPercentage + 
                    middleRanks[i] * middleTermPercentage + 
                    longRanks[i] * longTermPercentage;
            }

            // 保存完整結果
            var dailyRankedFileName = _configuration["AppSettings:DailyRankedFileName"];
            ArgumentException.ThrowIfNullOrWhiteSpace(dailyRankedFileName);
            
            string rankedResultJson = JsonSerializer.Serialize(symbolStrengths, new JsonSerializerOptions { WriteIndented = true });
            await Tools.WriteTextToFile(dailyResultFolderPath, dailyRankedFileName, rankedResultJson);

            // 返回按強度排序的完整結果（五日漸強需要完整清單）
            return symbolStrengths
                .OrderByDescending(s => s.Strength)
                .ToList();
        }

        /// <summary>
        /// 計算五日漸強標的 - 排除前N名後取前5名進步最多的
        /// </summary>
        public async Task<List<SymbolStrength>?> GetFiveDayImprovementSymbolsAsync(List<SymbolStrength> currentRankedSymbols, string dailyResultFolderPath, int rsTakeCount, int topCount = 5)
        {
            ArgumentNullException.ThrowIfNull(currentRankedSymbols);
            ArgumentException.ThrowIfNullOrWhiteSpace(dailyResultFolderPath);

            // 取得5天前的資料
            string fiveDaysAgoPath = Path.Combine(
                Path.GetDirectoryName(dailyResultFolderPath)!,
                DateTime.Now.AddDays(-5).ToString(SystemConstants.FolderDateFormat)
            );

            var dailyRankedFileName = _configuration["AppSettings:DailyRankedFileName"];
            ArgumentException.ThrowIfNullOrWhiteSpace(dailyRankedFileName);

            if (!Directory.Exists(fiveDaysAgoPath))
            {
                Console.WriteLine("找不到5天前的數據資料夾，無法計算五日漸強。");
                return null;
            }

            try
            {
                string fiveDaysAgoContent = await Tools.ReadFileContent(fiveDaysAgoPath, dailyRankedFileName);
                if (string.IsNullOrEmpty(fiveDaysAgoContent))
                {
                    Console.WriteLine("5天前的排名資料為空，無法計算五日漸強。");
                    return null;
                }

                var fiveDaysAgoSymbols = JsonSerializer.Deserialize<List<SymbolStrength>>(fiveDaysAgoContent);
                if (fiveDaysAgoSymbols == null || !fiveDaysAgoSymbols.Any())
                {
                    Console.WriteLine("無法解析5天前的排名資料，無法計算五日漸強。");
                    return null;
                }

                // 先過濾出排除前N名後的標的清單 (currentRankedSymbols已按Strength排序)
                var candidateSymbols = currentRankedSymbols
                    .Skip(rsTakeCount)
                    .ToList();

                // 計算五日漸強
                var improvementList = new List<(SymbolStrength symbol, double improvement)>();

                foreach (var currentSymbol in candidateSymbols)
                {
                    var fiveDaysAgoSymbol = fiveDaysAgoSymbols.FirstOrDefault(s => s.Symbol == currentSymbol.Symbol);
                    if (fiveDaysAgoSymbol != null)
                    {
                        double improvement = currentSymbol.Strength - fiveDaysAgoSymbol.Strength;
                        improvementList.Add((currentSymbol, improvement));
                    }
                }

                // 返回進步最多的前5名
                return improvementList
                    .OrderByDescending(x => x.improvement)
                    .Take(topCount)
                    .Select(x => x.symbol)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"計算五日漸強時發生錯誤: {ex.Message}");
                return null;
            }
        }

        #region 私有計算方法

        /// <summary>
        /// 計算百分位排名
        /// </summary>
        private static List<double> GetPercentRanks(List<double> values)
        {
            if (values == null || values.Count == 0)
                return new List<double>();

            var validValues = values.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToList();
            if (validValues.Count == 0)
                return values.Select(_ => 0.0).ToList();

            var sorted = validValues.OrderBy(v => v).ToList();
            return values.Select(v => {
                if (double.IsNaN(v) || double.IsInfinity(v))
                    return 0.0;
                if (sorted.Count == 1)
                    return 0.5; // 單一值時使用中位數排名
                return (double)sorted.IndexOf(v) / (sorted.Count - 1);
            }).ToList();
        }

        /// <summary>
        /// 計算回歸斜率
        /// </summary>
        private static double CalculateRegressionSlope(List<double> prices)
        {
            if (prices == null || prices.Count <= 1)
                return 0.0;

            int n = prices.Count;
            var x = Enumerable.Range(1, n).Select(i => (double)i).ToList();
            var y = prices;

            double sumX = x.Sum();
            double sumY = y.Sum();
            double sumXy = x.Zip(y, (xi, yi) => xi * yi).Sum();
            double sumX2 = x.Sum(xi => xi * xi);

            double denominator = n * sumX2 - sumX * sumX;
            if (Math.Abs(denominator) < double.Epsilon)
                return 0.0;

            double slope = (n * sumXy - sumX * sumY) / denominator;
            return double.IsFinite(slope) ? slope : 0.0;
        }

        #endregion
    }
}
