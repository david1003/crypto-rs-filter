using RSDailyFilter.Enums;

namespace RSDailyFilter.ExchangeService
{
    public interface IExchangeService
    {
        /// <summary>
        /// 取得當前交易所服務類型
        /// </summary>
        ExchangeServiceEnum ServiceType { get; }
        
        /// <summary>
        /// 非同步初始化
        /// </summary>
        /// <returns></returns>
        Task InitAsync();

        /// <summary>
        /// 取得所有合約交易對
        /// </summary>
        /// <returns></returns>
        Task<List<string>> GetTradingPairs();

        /// <summary>
        /// 取得指定合約的歷史價格
        /// </summary>
        /// <param name="symbol">合約名稱</param>
        /// <param name="interval">時區</param>
        /// <param name="letterEnum">時區代碼列舉</param>
        /// <param name="limit">數量</param>
        /// <returns></returns>
        Task<List<double>> GetHistoricalPrices(string symbol, int interval, PriceIntervalLetterEnum letterEnum, int limit);

        /// <summary>
        /// 將傳入的合約清單轉成TV觀察清單格式
        /// </summary>
        /// <param name="symbols"></param>
        /// <returns></returns>
        string ConvertSymbolsToTradingViewFormat(List<string> symbols);
    }

    public enum PriceIntervalLetterEnum
    {
        /// <summary>
        /// Day
        /// </summary>
        D = 0,
        /// <summary>
        /// Hour
        /// </summary>
        H = 1,
        /// <summary>
        /// Minute
        /// </summary>
        M = 2,
        /// <summary>
        /// Second
        /// </summary>
        S = 3,
    }
}
