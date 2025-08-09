using RSDailyFilter.Models;

namespace RSDailyFilter.Services;

/// <summary>
/// 通知服務介面 - 處理所有 Telegram 通知相關功能
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 發送RS排名和五日漸強結果到Telegram
    /// </summary>
    /// <param name="rankedSymbols">RS排名結果</param>
    /// <param name="fiveDayImprovement">五日漸強結果（可選）</param>
    Task SendRankingResultsAsync(List<SymbolStrength> rankedSymbols, List<SymbolStrength>? fiveDayImprovement);

    /// <summary>
    /// 發送合約變更通知到Telegram
    /// </summary>
    /// <param name="message">通知訊息</param>
    Task SendContractUpdateNotificationAsync(string message);
}
