using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using RSDailyFilter.Helper;
using RSDailyFilter.Common;
using RSDailyFilter.Enums;
using RSDailyFilter.Extensions;
using RSDailyFilter.Services;
using RSDailyFilter.ExchangeService;

namespace RSDailyFilter
{
    public class Program
    {
        private static readonly string AppBasePath = AppContext.BaseDirectory;
        
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("RS Daily Filter Start.");

                // Build configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppBasePath) 
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) 
                    .Build();

                ArgumentNullException.ThrowIfNull(configuration);

                string? folderPath = configuration["AppSettings:RSRankDailyResultPath"];
                ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

                // 設定服務容器
                var services = new ServiceCollection();
                services.AddRsDailyFilterServices(configuration);
                
                var serviceProvider = services.BuildServiceProvider();
                
                // 取得服務
                var exchangeService = serviceProvider.GetRequiredService<IExchangeService>();
                var rsAnalysisService = serviceProvider.GetRequiredService<IRsAnalysisService>();
                var notificationService = serviceProvider.GetRequiredService<INotificationService>();
                
                await exchangeService.InitAsync();

                // 取得交易日期資料夾路徑
                string dailyResultFolderPath = Path.Combine(folderPath, DateTime.Now.ToString(SystemConstants.FolderDateFormat));
                if (!Directory.Exists(dailyResultFolderPath))
                {
                    Directory.CreateDirectory(dailyResultFolderPath);
                }

                //取得合約清單
                var symbols = await exchangeService.GetTradingPairs();

                //檔案模式不需要檢查合約清單是否有異動
                if (exchangeService.ServiceType != ExchangeServiceEnum.File)
                {
                    var contractChangeMessage = await rsAnalysisService.CompareContractSymbolsAsync(symbols, dailyResultFolderPath);
                    if (!string.IsNullOrEmpty(contractChangeMessage))
                    {
                        await notificationService.SendContractUpdateNotificationAsync(contractChangeMessage);
                    }
                }

                //計算RS排名
                int rsTakeCount = Convert.ToInt32(configuration["AppSettings:RSTakeCount"]);
                var rankedSymbols = await rsAnalysisService.GetRsRankedSymbolsAsync(symbols, dailyResultFolderPath, rsTakeCount);

                //檢查是否有有效的RS排名結果
                if (rankedSymbols == null || !rankedSymbols.Any())
                {
                    Console.WriteLine("警告：沒有有效的RS排名數據，跳過發送訊息。");
                    return;
                }

                //計算五日漸強
                var fiveDayImprovement = await rsAnalysisService.GetFiveDayImprovementSymbolsAsync(rankedSymbols, dailyResultFolderPath, rsTakeCount, 5);

                //發送排名結果訊息 (只傳遞前N名)
                var topRankedSymbols = rankedSymbols.Take(rsTakeCount).ToList();
                await notificationService.SendRankingResultsAsync(topRankedSymbols, fiveDayImprovement);

                //刪除十天前的資料夾
                if (exchangeService.ServiceType != ExchangeServiceEnum.File)
                {
                    DeleteOldResultFolders(folderPath, 30);
                }

                Console.WriteLine("RS Daily Filter End.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error exception: {e}");
                throw;
            }
        }

        /// <summary>
        /// 刪除結果資料夾下，n天前的資料夾
        /// </summary>
        private static void DeleteOldResultFolders(string resultRootPath, int daysToKeep = 10)
        {
            if (!Directory.Exists(resultRootPath))
                return;

            var cutoffDate = DateTime.Now.Date.AddDays(-daysToKeep);

            foreach (var folder in Directory.GetDirectories(resultRootPath))
            {
                var folderName = Path.GetFileName(folder);
                if (DateTime.TryParseExact(folderName, SystemConstants.FolderDateFormat, null,
                        System.Globalization.DateTimeStyles.None, out DateTime folderDate))
                {
                    if (folderDate < cutoffDate)
                    {
                        try
                        {
                            Directory.Delete(folder, recursive: true);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to delete folder {folder}: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}
