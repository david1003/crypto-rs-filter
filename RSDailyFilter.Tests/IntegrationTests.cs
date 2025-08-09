using Microsoft.Extensions.Configuration;
using Moq;
using RSDailyFilter.ExchangeService;

namespace RSDailyFilter.Tests;

/// <summary>
/// 整合測試 - 測試主要工作流程的整合
/// </summary>
public class IntegrationTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly IConfiguration _configuration;

    public IntegrationTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), "RSDailyFilterIntegrationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBasePath);

        // 設置測試配置
        var configData = new Dictionary<string, string>
        {
            {"AppSettings:RSRankDailyResultPath", _testBasePath},
            {"AppSettings:CurrentTermDays", "7"},
            {"AppSettings:ShortDays", "5"},
            {"AppSettings:MiddleDays", "7"},
            {"AppSettings:LongDays", "10"},
            {"AppSettings:CurrentTermPercentage", "0.2"},
            {"AppSettings:ShortTermPercentage", "0.4"},
            {"AppSettings:MiddleTermPercentage", "0.1"},
            {"AppSettings:LongTermPercentage", "0.3"},
            {"AppSettings:RSTakeCount", "30"},
            {"AppSettings:IgnoreCryptoList", "BTCDOMUSDT,USDCUSDT"},
            {"AppSettings:SymbolPriceFileName", "TestPrices.txt"},
            {"AppSettings:ExchangeServiceType", "File"},
            {"AppSettings:FileExchangeTargetDate", "2024-01-15"},
            {"AppSettings:TelegramBotToken", "test_token"},
            {"AppSettings:TgRsRankChatId", "test_chat_id"},
            {"AppSettings:TgContractUpdateChatId", "test_contract_chat_id"},
            {"AppSettings:SymbolOutputFileName", "TestContracts.txt"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    [Fact]
    public async Task FileExchangeService_EndToEndWorkflow_ShouldProcessDataCorrectly()
    {
        // Arrange
        var testDate = new DateTime(2024, 1, 15);
        var symbolPriceFileName = "TestPrices.txt";
        var testDateFolder = Path.Combine(_testBasePath, testDate.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(testDateFolder);

        // 創建測試價格數據
        var testPriceContent = 
            "BTCUSDT|42000.0,42500.0,43000.0,43500.0,44000.0,44500.0,45000.0,45500.0,46000.0,46500.0\n" +
            "ETHUSDT|2500.0,2550.0,2600.0,2650.0,2700.0,2750.0,2800.0,2850.0,2900.0,2950.0\n" +
            "ADAUSDT|0.45,0.46,0.47,0.48,0.49,0.50,0.51,0.52,0.53,0.54\n" +
            "BTCDOMUSDT|50.0,50.1,50.2,50.3,50.4,50.5,50.6,50.7,50.8,50.9"; // 這個應該被忽略

        var priceFilePath = Path.Combine(testDateFolder, symbolPriceFileName);
        await File.WriteAllTextAsync(priceFilePath, testPriceContent);

        // Act
        var exchangeService = new FileExchangeService(_testBasePath, testDate, symbolPriceFileName);
        await exchangeService.InitAsync();
        
        var tradingPairs = await exchangeService.GetTradingPairs();
        var btcPrices = await exchangeService.GetHistoricalPrices("BTCUSDT", 1, PriceIntervalLetterEnum.D, 10);
        var ethPrices = await exchangeService.GetHistoricalPrices("ETHUSDT", 1, PriceIntervalLetterEnum.D, 10);

        // Assert
        Assert.NotNull(tradingPairs);
        Assert.Equal(4, tradingPairs.Count); // 包含被忽略的 BTCDOMUSDT
        Assert.Contains("BTCUSDT", tradingPairs);
        Assert.Contains("ETHUSDT", tradingPairs);
        Assert.Contains("ADAUSDT", tradingPairs);
        Assert.Contains("BTCDOMUSDT", tradingPairs);

        Assert.NotNull(btcPrices);
        Assert.Equal(10, btcPrices.Count);
        Assert.Equal(42000.0, btcPrices[0]);
        Assert.Equal(46500.0, btcPrices[9]);

        Assert.NotNull(ethPrices);
        Assert.Equal(10, ethPrices.Count);
        Assert.Equal(2500.0, ethPrices[0]);
        Assert.Equal(2950.0, ethPrices[9]);
    }

    [Fact]
    public void TradingViewFormat_ShouldGenerateCorrectFormat()
    {
        // Arrange
        var exchangeService = new FileExchangeService(_testBasePath, DateTime.Now, "test.txt");
        var symbols = new List<string> { "BTCUSDT", "ETHUSDT", "ADAUSDT" };

        // Act
        var result = exchangeService.ConvertSymbolsToTradingViewFormat(symbols);

        // Assert
        Assert.Equal("BINANCE:BTCUSDT.P,BINANCE:ETHUSDT.P,BINANCE:ADAUSDT.P", result);
    }

    [Fact]
    public void Configuration_ShouldLoadAllRequiredSettings()
    {
        // Assert
        Assert.Equal(_testBasePath, _configuration["AppSettings:RSRankDailyResultPath"]);
        Assert.Equal("7", _configuration["AppSettings:CurrentTermDays"]);
        Assert.Equal("5", _configuration["AppSettings:ShortDays"]);
        Assert.Equal("0.2", _configuration["AppSettings:CurrentTermPercentage"]);
        Assert.Equal("30", _configuration["AppSettings:RSTakeCount"]);
        Assert.Equal("BTCDOMUSDT,USDCUSDT", _configuration["AppSettings:IgnoreCryptoList"]);
        Assert.Equal("File", _configuration["AppSettings:ExchangeServiceType"]);
    }

    [Fact]
    public async Task FileOperations_EndToEndTest_ShouldWorkCorrectly()
    {
        // Arrange
        var testContent = "Test content for integration test";
        var fileName = "integration_test.txt";
        var folderPath = Path.Combine(_testBasePath, "TestFolder");

        // Act
        var writtenFilePath = await RSDailyFilter.Common.Tools.WriteTextToFile(folderPath, fileName, testContent);
        var readContent = await RSDailyFilter.Common.Tools.ReadFileContent(folderPath, fileName);

        // Assert
        Assert.True(File.Exists(writtenFilePath));
        Assert.Equal(testContent, readContent);
        Assert.Equal(Path.Combine(folderPath, fileName), writtenFilePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, true);
        }
    }
}
