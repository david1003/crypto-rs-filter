namespace RSDailyFilter.Models
{
    /// <summary>
    /// 符號強度模型
    /// </summary>
    public class SymbolStrength
    {
        /// <summary>
        /// 符號名稱
        /// </summary>
        public string Symbol { get; set; } = "";

        /// <summary>
        /// 當前期RS值
        /// </summary>
        public double CurrentTermRs { get; set; }

        /// <summary>
        /// 短期RS值
        /// </summary>
        public double ShortRs { get; set; }

        /// <summary>
        /// 中期RS值
        /// </summary>
        public double MiddleRs { get; set; }

        /// <summary>
        /// 長期RS值
        /// </summary>
        public double LongRs { get; set; }

        /// <summary>
        /// 當前期RS排名
        /// </summary>
        public double CurrentTermRsRank { get; set; }

        /// <summary>
        /// 短期RS排名
        /// </summary>
        public double ShortRsRank { get; set; }

        /// <summary>
        /// 中期RS排名
        /// </summary>
        public double MiddleRsRank { get; set; }

        /// <summary>
        /// 長期RS排名
        /// </summary>
        public double LongRsRank { get; set; }

        /// <summary>
        /// 綜合強度
        /// </summary>
        public double Strength { get; set; }
    }
}
