using System.Text;
using Microsoft.Extensions.Configuration;
using RSDailyFilter.Common;
using RSDailyFilter.ExchangeService;
using RSDailyFilter.Helper;
using RSDailyFilter.Models;

namespace RSDailyFilter.Services;

/// <summary>
/// 通知服務實現 - 處理所有 Telegram 通知相關功能
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IConfiguration _configuration;
    private readonly TelegramHelper _telegramHelper;
    private readonly IExchangeService _exchangeService;

    public NotificationService(
        IConfiguration configuration, 
        TelegramHelper telegramHelper,
        IExchangeService exchangeService)
    {
        _configuration = configuration;
        _telegramHelper = telegramHelper;
        _exchangeService = exchangeService;
    }

    /// <summary>
    /// 發送RS排名和五日漸強結果到Telegram
    /// </summary>
    public async Task SendRankingResultsAsync(List<SymbolStrength> rankedSymbols, List<SymbolStrength>? fiveDayImprovement)
    {
        int rsResultTakeCount = Convert.ToInt32(_configuration["AppSettings:RSTakeCount"]);
        string? resultFolderPath = _configuration["AppSettings:RSRankDailyResultPath"];
        string? tgRsRankChannelChatId = _configuration["AppSettings:TgRsRankChatId"];
        
        ArgumentException.ThrowIfNullOrWhiteSpace(tgRsRankChannelChatId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resultFolderPath);

        // 檢查是否有有效的排名數據
        if (rankedSymbols == null || !rankedSymbols.Any())
        {
            Console.WriteLine("警告：沒有有效的RS排名數據，無法發送訊息。");
            return;
        }

        string dailyResultFolderPath = Path.Combine(resultFolderPath, DateTime.Now.ToString(SystemConstants.FolderDateFormat));

        // 準備RS排名訊息
        var topRankedSymbols = rankedSymbols.Take(rsResultTakeCount).ToList();
        if (!topRankedSymbols.Any())
        {
            Console.WriteLine("警告：沒有足夠的排名數據可供發送。");
            return;
        }

        // 準備完整的訊息內容
        string rsRankedContractMessage = string.Join($"{SystemConstants.Splitter} ", topRankedSymbols.Select(s => s.Symbol.Replace("USDT", string.Empty)));
        string completeMessage = $"RS Rank Top {topRankedSymbols.Count}: \r\n{rsRankedContractMessage}";
        
        // 如果有五日漸強資料，加入到訊息中
        if (fiveDayImprovement != null && fiveDayImprovement.Any())
        {
            string fiveDayMessage = string.Join($"{SystemConstants.Splitter} ", fiveDayImprovement.Select(s => s.Symbol.Replace("USDT", string.Empty)));
            completeMessage += $"\r\n\r\n五日漸強：\r\n{fiveDayMessage}";
        }

        // 創建包含RS排名和五日漸強的TradingView格式檔案
        StringBuilder tradingViewContent = new StringBuilder();
        
        // 加入RS排名到TradingView格式
        string rsSymbolsForTv = _exchangeService.ConvertSymbolsToTradingViewFormat(topRankedSymbols.Select(s => s.Symbol).ToList());
        tradingViewContent.AppendLine($"###RS_TOP_30,{rsSymbolsForTv}");
        
        // 如果有五日漸強資料，加入到TradingView格式
        if (fiveDayImprovement != null && fiveDayImprovement.Any())
        {
            string fiveDaySymbolsForTv = _exchangeService.ConvertSymbolsToTradingViewFormat(fiveDayImprovement.Select(s => s.Symbol).ToList());
            tradingViewContent.AppendLine($"###五日漸強,{fiveDaySymbolsForTv}");
        }

        var rsResultFilePath = await Tools.WriteTextToFile(dailyResultFolderPath, $"0.RS_{DateTime.Now.ToString(SystemConstants.RsResultFileDateFormat)}{SystemConstants.OutputFileExtension}", tradingViewContent.ToString());

        // 發送RS排名結果（包含五日漸強訊息）
        await _telegramHelper.SendFileToTelegramChannelAsync(tgRsRankChannelChatId, rsResultFilePath, completeMessage);

        // 輸出發送狀態訊息
        if (fiveDayImprovement != null && fiveDayImprovement.Any())
        {
            Console.WriteLine("已發送RS排名和五日漸強結果。");
        }
        else
        {
            Console.WriteLine("已發送RS排名結果。資訊：沒有五日漸強數據可供發送。");
        }
    }

    /// <summary>
    /// 發送合約變更通知到Telegram
    /// </summary>
    public async Task SendContractUpdateNotificationAsync(string message)
    {
        string? tgContractUpdateChannelChatId = _configuration["AppSettings:TgContractUpdateChatId"];
        string? resultFolderPath = _configuration["AppSettings:RSRankDailyResultPath"];
        ArgumentException.ThrowIfNullOrWhiteSpace(tgContractUpdateChannelChatId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resultFolderPath);

        // 取得當天所有交易對清單用於 TradingView 附件
        var allSymbols = await _exchangeService.GetTradingPairs();
        
        // 獲取忽略清單並過濾
        string? ignoreCryptoListString = _configuration["AppSettings:IgnoreCryptoList"];
        List<string> ignoreCryptoList = string.IsNullOrEmpty(ignoreCryptoListString)
            ? new List<string>()
            : ignoreCryptoListString.Split(',').Select(s => s.Trim()).ToList();
        
        var filteredSymbols = allSymbols.Where(s => !ignoreCryptoList.Contains(s)).ToList();

        // 寫入 TradingView 清單
        string tvFileName = $"contractForTv{SystemConstants.OutputFileExtension}";
        string tvContractResult = _exchangeService.ConvertSymbolsToTradingViewFormat(filteredSymbols);
        string tvFileFullPath = await Tools.WriteTextToFile(resultFolderPath, tvFileName, tvContractResult);

        // 傳到 TG
        string tgContractFileName = $"{DateTime.Now:MMdd} USDT Contracts.txt";

        // 發送帶有附件的通知
        await _telegramHelper.SendFileToTelegramChannelAsync(tgContractUpdateChannelChatId, tvFileFullPath, message, tgContractFileName);
        Console.WriteLine("已發送合約變更通知和 TradingView 附件。");
    }
}
