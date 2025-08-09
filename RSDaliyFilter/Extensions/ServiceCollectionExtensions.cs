using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using RSDailyFilter.ExchangeService;
using RSDailyFilter.Services;
using RSDailyFilter.Helper;
using RSDailyFilter.Enums;

namespace RSDailyFilter.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRsDailyFilterServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 註冊 IConfiguration
            services.AddSingleton(configuration);
            
            // 註冊 TelegramHelper
            var botToken = configuration["AppSettings:TelegramBotToken"];
            ArgumentException.ThrowIfNullOrWhiteSpace(botToken);
            services.AddSingleton(provider => new TelegramHelper(botToken));

            // 註冊 ExchangeService
            if (!Enum.TryParse(configuration["AppSettings:ExchangeServiceType"], true, out ExchangeServiceEnum exchangeServiceMode))
            {
                exchangeServiceMode = ExchangeServiceEnum.Binance;
            }

            switch (exchangeServiceMode)
            {
                case ExchangeServiceEnum.File:
                    DateTime? targetDate = string.IsNullOrWhiteSpace(configuration["AppSettings:FileExchangeTargetDate"])
                        ? null
                        : DateTime.Parse(configuration["AppSettings:FileExchangeTargetDate"]!);
                    string? symbolPriceFileName = configuration["AppSettings:SymbolPriceFileName"];
                    string? folderPath = configuration["AppSettings:RSRankDailyResultPath"];
                    
                    ArgumentNullException.ThrowIfNull(targetDate);
                    ArgumentException.ThrowIfNullOrWhiteSpace(symbolPriceFileName);
                    ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
                    
                    services.AddSingleton<IExchangeService>(provider => 
                        new FileExchangeService(folderPath, targetDate.Value, symbolPriceFileName));
                    break;

                case ExchangeServiceEnum.Binance:
                default:
                    services.AddHttpClient();
                    services.AddTransient<IExchangeService>(provider => 
                    {
                        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                        var httpClient = httpClientFactory.CreateClient();
                        return new BinanceExchangeService(httpClient);
                    });
                    break;
            }

            // 註冊業務服務
            services.AddTransient<IRsAnalysisService, RsAnalysisService>();
            services.AddTransient<INotificationService, NotificationService>();

            return services;
        }
    }
}
