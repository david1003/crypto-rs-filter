using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSDailyFilter.Common
{
    public static class SystemConstants
    {
        /// <summary>
        /// 資料夾日期格式
        /// </summary>
        public static readonly string FolderDateFormat = "yyyy-MM-dd";
        /// <summary>
        /// rs結果檔日期格式
        /// </summary>
        public static readonly string RsResultFileDateFormat = "yyyyMMdd";
        /// <summary>
        /// 輸出副檔名
        /// </summary>
        public static readonly string OutputFileExtension = ".txt";
        /// <summary>
        /// 
        /// </summary>
        public static readonly string Splitter = ",";
        /// <summary>
        /// 標的價格分隔符號
        /// </summary>
        public static readonly string PriceSymbolSplitter = "|";
    }
}
