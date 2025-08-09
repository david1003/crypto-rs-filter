using Microsoft.Extensions.Configuration;
using RSDailyFilter.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace RSDailyFilter.Tests;

/// <summary>
/// 配置驗證測試 - 確保所有必要的配置參數都存在且有效
/// </summary>
public class ConfigurationValidationTests
{
    private readonly IConfiguration _configuration;

    public ConfigurationValidationTests()
    {
        // 載入實際的配置檔案進行測試
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
    }

    [Fact]
    public void AppSettings_ShouldContainAllRequiredDayParameters()
    {
        // Arrange & Act & Assert
        var currentTermDays = _configuration["AppSettings:CurrentTermDays"];
        var shortDays = _configuration["AppSettings:ShortDays"];
        var middleDays = _configuration["AppSettings:MiddleDays"];
        var longDays = _configuration["AppSettings:LongDays"];

        Assert.NotNull(currentTermDays);
        Assert.NotNull(shortDays);
        Assert.NotNull(middleDays);
        Assert.NotNull(longDays);

        // 驗證可以轉換為整數且大於0
        Assert.True(int.TryParse(currentTermDays, out int currentDays) && currentDays > 0);
        Assert.True(int.TryParse(shortDays, out int shortD) && shortD > 0);
        Assert.True(int.TryParse(middleDays, out int middleD) && middleD > 0);
        Assert.True(int.TryParse(longDays, out int longD) && longD > 0);
    }

    [Fact]
    public void AppSettings_ShouldContainRSTakeCount()
    {
        // Arrange & Act
        var rsTakeCount = _configuration["AppSettings:RSTakeCount"];

        // Assert
        Assert.NotNull(rsTakeCount);
        Assert.True(int.TryParse(rsTakeCount, out int takeCount) && takeCount > 0, 
            "RSTakeCount should be a positive integer");
    }

    [Fact]
    public void AppSettings_ShouldContainAllRequiredPercentageParameters()
    {
        // Arrange & Act & Assert
        var currentTermPercentage = _configuration["AppSettings:CurrentTermPercentage"];
        var shortTermPercentage = _configuration["AppSettings:ShortTermPercentage"];
        var middleTermPercentage = _configuration["AppSettings:MiddleTermPercentage"];
        var longTermPercentage = _configuration["AppSettings:LongTermPercentage"];

        Assert.NotNull(currentTermPercentage);
        Assert.NotNull(shortTermPercentage);
        Assert.NotNull(middleTermPercentage);
        Assert.NotNull(longTermPercentage);

        // 驗證可以轉換為雙精度且在合理範圍內
        Assert.True(double.TryParse(currentTermPercentage, out double currentPct) && currentPct >= 0 && currentPct <= 1);
        Assert.True(double.TryParse(shortTermPercentage, out double shortPct) && shortPct >= 0 && shortPct <= 1);
        Assert.True(double.TryParse(middleTermPercentage, out double middlePct) && middlePct >= 0 && middlePct <= 1);
        Assert.True(double.TryParse(longTermPercentage, out double longPct) && longPct >= 0 && longPct <= 1);
    }

    [Fact]
    public void AppSettings_PercentagesShouldSumToOne()
    {
        // Arrange
        var currentTermPercentage = Convert.ToDouble(_configuration["AppSettings:CurrentTermPercentage"]);
        var shortTermPercentage = Convert.ToDouble(_configuration["AppSettings:ShortTermPercentage"]);
        var middleTermPercentage = Convert.ToDouble(_configuration["AppSettings:MiddleTermPercentage"]);
        var longTermPercentage = Convert.ToDouble(_configuration["AppSettings:LongTermPercentage"]);

        // Act
        var totalPercentage = currentTermPercentage + shortTermPercentage + middleTermPercentage + longTermPercentage;

        // Assert
        Assert.Equal(1.0, totalPercentage, 2); // 精確到小數點後2位
    }

    [Fact]
    public void AppSettings_ShouldContainAllRequiredFileParameters()
    {
        // Arrange & Act & Assert
        var rsRankDailyResultPath = _configuration["AppSettings:RSRankDailyResultPath"];
        var symbolOutputFileName = _configuration["AppSettings:SymbolOutputFileName"];
        var symbolPriceFileName = _configuration["AppSettings:SymbolPriceFileName"];
        var dailyRankedFileName = _configuration["AppSettings:DailyRankedFileName"];

        Assert.NotNull(rsRankDailyResultPath);
        Assert.NotNull(symbolOutputFileName);
        Assert.NotNull(symbolPriceFileName);
        Assert.NotNull(dailyRankedFileName);

        Assert.NotEmpty(rsRankDailyResultPath);
        Assert.NotEmpty(symbolOutputFileName);
        Assert.NotEmpty(symbolPriceFileName);
        Assert.NotEmpty(dailyRankedFileName);
    }

    [Fact]
    public void AppSettings_ShouldContainAllRequiredTelegramParameters()
    {
        // Arrange & Act & Assert
        var tgBotToken = _configuration["AppSettings:TelegramBotToken"];
        var tgRsRankChatId = _configuration["AppSettings:TgRsRankChatId"];
        var tgContractUpdateChatId = _configuration["AppSettings:TgContractUpdateChatId"];

        Assert.NotNull(tgBotToken);
        Assert.NotNull(tgRsRankChatId);
        Assert.NotNull(tgContractUpdateChatId);

        Assert.NotEmpty(tgBotToken);
        Assert.NotEmpty(tgRsRankChatId);
        Assert.NotEmpty(tgContractUpdateChatId);
    }

    [Fact]
    public void AppSettings_RSTakeCount_ShouldBeValidInteger()
    {
        // Arrange & Act
        var rsTakeCount = _configuration["AppSettings:RSTakeCount"];

        // Assert
        Assert.NotNull(rsTakeCount);
        Assert.True(int.TryParse(rsTakeCount, out int takeCount) && takeCount > 0);
        Assert.True(takeCount <= 100); // 合理的上限
    }

    [Fact]
    public void AppSettings_ExchangeServiceType_ShouldBeValid()
    {
        // Arrange & Act
        var exchangeServiceType = _configuration["AppSettings:ExchangeServiceType"];

        // Assert
        Assert.NotNull(exchangeServiceType);
        Assert.True(exchangeServiceType == "Binance" || exchangeServiceType == "File", 
            $"ExchangeServiceType 應該是 'Binance' 或 'File'，但得到: {exchangeServiceType}");
    }

    [Fact]
    public void ServiceCollection_ShouldRegisterAllServicesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_configuration);

        // Act - 這應該不會拋出異常
        var exception = Record.Exception(() => services.AddRsDailyFilterServices(_configuration));

        // Assert
        Assert.Null(exception);
        
        var serviceProvider = services.BuildServiceProvider();
        
        // 驗證所有服務都能成功解析
        Assert.NotNull(serviceProvider.GetService<RSDailyFilter.ExchangeService.IExchangeService>());
        Assert.NotNull(serviceProvider.GetService<RSDailyFilter.Services.IRsAnalysisService>());
        Assert.NotNull(serviceProvider.GetService<RSDailyFilter.Services.INotificationService>());
        Assert.NotNull(serviceProvider.GetService<RSDailyFilter.Helper.TelegramHelper>());
    }

    [Theory]
    [InlineData("AppSettings:CurrentTermDays")]
    [InlineData("AppSettings:ShortDays")]
    [InlineData("AppSettings:MiddleDays")]
    [InlineData("AppSettings:LongDays")]
    [InlineData("AppSettings:CurrentTermPercentage")]
    [InlineData("AppSettings:ShortTermPercentage")]
    [InlineData("AppSettings:MiddleTermPercentage")]
    [InlineData("AppSettings:LongTermPercentage")]
    [InlineData("AppSettings:RSTakeCount")]
    [InlineData("AppSettings:RSRankDailyResultPath")]
    [InlineData("AppSettings:SymbolOutputFileName")]
    [InlineData("AppSettings:SymbolPriceFileName")]
    [InlineData("AppSettings:DailyRankedFileName")]
    [InlineData("AppSettings:TelegramBotToken")]
    [InlineData("AppSettings:TgRsRankChatId")]
    [InlineData("AppSettings:TgContractUpdateChatId")]
    [InlineData("AppSettings:ExchangeServiceType")]
    public void AppSettings_RequiredParameter_ShouldExist(string parameterKey)
    {
        // Arrange & Act
        var value = _configuration[parameterKey];

        // Assert
        Assert.NotNull(value);
        Assert.NotEmpty(value);
    }
}
