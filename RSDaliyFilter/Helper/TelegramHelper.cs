using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace RSDailyFilter.Helper
{
    public class TelegramHelper
    {
        private readonly string _botToken;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="botToken"></param>
        public TelegramHelper(string botToken)
        {
            _botToken = botToken;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// 傳送訊息至指定Channel
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendMessageToChannelAsync(string chatId, string message)
        {
            string url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

            var parameters = new Dictionary<string, string>
            {
                { "chat_id", chatId },
                { "text", message }
            };


            try
            {
                var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("訊息已成功傳送到頻道！");
                }
                else
                {
                    Console.WriteLine($"TG發送失敗，狀態碼: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendMessageToChannelAsync發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 傳送檔案至指定Channel
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="filePath"></param>
        /// <param name="caption"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task SendFileToTelegramChannelAsync(
            string chatId,
            string filePath,
            string caption = "",
            string fileName = "")
        {
            using var client = new HttpClient();

            // Telegram Bot API URL
            string telegramApiUrl = $"https://api.telegram.org/bot{_botToken}/sendDocument";
            string sendFileName = string.IsNullOrWhiteSpace(fileName) ? Path.GetFileName(filePath) : fileName;
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(chatId), "chat_id"); // 頻道 ID
            form.Add(new StringContent(caption), "caption"); // 訊息描述（可選）

            // 讀取檔案內容
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

            // 手動設置 Content-Disposition，並使用 UTF-8 編碼檔案名稱
            string headerValue = $"form-data; name=\"document\"; filename=\"{sendFileName}\"; filename*=UTF-8''{Uri.EscapeDataString(sendFileName)}";
            fileContent.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse(headerValue);

            form.Add(fileContent);

            try
            {
                HttpResponseMessage response = await client.PostAsync(telegramApiUrl, form);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("檔案已成功傳送到頻道！");
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"TG發送失敗，狀態碼: {response.StatusCode}");
                    Console.WriteLine($"錯誤詳情: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendFileToTelegramChannelAsync發生錯誤: {ex.Message}");
            }
        }
    }
}
