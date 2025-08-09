using RSDailyFilter.Models;

namespace RSDailyFilter.Services
{
    /// <summary>
    /// RS 分析服務介面 - 包含合約比較、RS計算、五日漸強功能
    /// </summary>
    public interface IRsAnalysisService
    {
        /// <summary>
        /// 比較當前合約符號是否與昨日不同，返回比較結果
        /// </summary>
        /// <param name="symbols">當前合約符號列表</param>
        /// <param name="dailyResultFolderPath">日結果資料夾路徑</param>
        /// <returns>合約變更訊息，如果沒有變更則返回 null</returns>
        Task<string?> CompareContractSymbolsAsync(List<string> symbols, string dailyResultFolderPath);

        /// <summary>
        /// 計算並取得 RS 排名的標的清單
        /// </summary>
        /// <param name="symbols">標的清單</param>
        /// <param name="dailyResultFolderPath">日結果資料夾路徑</param>
        /// <param name="topCount">取前幾名</param>
        /// <returns>RS排名後的標的清單</returns>
        Task<List<SymbolStrength>> GetRsRankedSymbolsAsync(List<string> symbols, string dailyResultFolderPath, int topCount = 30);

        /// <summary>
        /// 計算五日漸強標的
        /// </summary>
        /// <param name="currentRankedSymbols">當前排名符號</param>
        /// <param name="dailyResultFolderPath">日結果資料夾路徑</param>
        /// <param name="rsTakeCount">RS排名要排除的前N名數量</param>
        /// <param name="topCount">取前幾名</param>
        /// <returns>五日漸強標的清單</returns>
        Task<List<SymbolStrength>?> GetFiveDayImprovementSymbolsAsync(List<SymbolStrength> currentRankedSymbols, string dailyResultFolderPath, int rsTakeCount, int topCount = 5);
    }
}
