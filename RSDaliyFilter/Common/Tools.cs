using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSDailyFilter.Common
{
    public static class Tools
    {
        /// <summary>
        /// 將文字寫入檔案
        /// </summary>
        /// <param name="folderFullPath"></param>
        /// <param name="fileName"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static async Task<string> WriteTextToFile(string? folderFullPath, string fileName, string text)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(folderFullPath);

            if (!Directory.Exists(folderFullPath))
            {
                Directory.CreateDirectory(folderFullPath);
            }

            string filePath = Path.Combine(folderFullPath, $"{fileName}");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            await File.AppendAllTextAsync(filePath, text);
            return filePath;
        }

        /// <summary>
        /// 讀取檔案內容
        /// </summary>
        /// <param name="folderFullPath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task<string> ReadFileContent(string folderFullPath, string fileName)
        {
            string filePath = Path.Combine(folderFullPath, $"{fileName}");
            if (!File.Exists(filePath))
            {
                return string.Empty;
            }
            return await File.ReadAllTextAsync(filePath);
        }
    }
}
