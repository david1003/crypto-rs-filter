using RSDailyFilter.ExchangeService;
using RSDailyFilter.Common;
using System.IO;

namespace RSDailyFilter.Tests.ExchangeService;

public class FileExchangeServiceTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly string _testDateFolder;
    private readonly string _symbolPriceFileName = "TestPrices.txt";
    private readonly DateTime _testDate = new DateTime(2024, 1, 15);

    public FileExchangeServiceTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), "RSDailyFilterTests", Guid.NewGuid().ToString());
        _testDateFolder = Path.Combine(_testBasePath, _testDate.ToString(SystemConstants.FolderDateFormat));
        Directory.CreateDirectory(_testDateFolder);
    }

    [Fact]
    public async Task InitAsync_WithValidPriceFile_ShouldLoadSymbolPrices()
    {
        // Arrange
        var testPriceContent = "BTCUSDT|47000.0,47500.0,48000.0,48500.0,49000.0\n" +
                              "ETHUSDT|3000.0,3100.0,3200.0,3300.0,3400.0\n" +
                              "ADAUSDT|1.20,1.25,1.30,1.35,1.40";

        var priceFilePath = Path.Combine(_testDateFolder, _symbolPriceFileName);
        await File.WriteAllTextAsync(priceFilePath, testPriceContent);

        var service = new FileExchangeService(_testBasePath, _testDate, _symbolPriceFileName);

        // Act
        await service.InitAsync();
        var tradingPairs = await service.GetTradingPairs();

        // Assert
        Assert.NotNull(tradingPairs);
        Assert.Equal(3, tradingPairs.Count);
        Assert.Contains("BTCUSDT", tradingPairs);
        Assert.Contains("ETHUSDT", tradingPairs);
        Assert.Contains("ADAUSDT", tradingPairs);
    }

    [Fact]
    public async Task GetHistoricalPrices_WithValidSymbol_ShouldReturnPrices()
    {
        // Arrange
        var testPriceContent = "BTCUSDT|47000.0,47500.0,48000.0,48500.0,49000.0\n" +
                              "ETHUSDT|3000.0,3100.0,3200.0,3300.0,3400.0";

        var priceFilePath = Path.Combine(_testDateFolder, _symbolPriceFileName);
        await File.WriteAllTextAsync(priceFilePath, testPriceContent);

        var service = new FileExchangeService(_testBasePath, _testDate, _symbolPriceFileName);
        await service.InitAsync();

        // Act
        var btcPrices = await service.GetHistoricalPrices("BTCUSDT", 1, PriceIntervalLetterEnum.D, 10);
        var ethPrices = await service.GetHistoricalPrices("ETHUSDT", 1, PriceIntervalLetterEnum.D, 10);

        // Assert
        Assert.NotNull(btcPrices);
        Assert.Equal(5, btcPrices.Count);
        Assert.Equal(47000.0, btcPrices[0]);
        Assert.Equal(49000.0, btcPrices[4]);

        Assert.NotNull(ethPrices);
        Assert.Equal(5, ethPrices.Count);
        Assert.Equal(3000.0, ethPrices[0]);
        Assert.Equal(3400.0, ethPrices[4]);
    }

    [Fact]
    public async Task GetHistoricalPrices_WithInvalidSymbol_ShouldReturnEmptyList()
    {
        // Arrange
        var testPriceContent = "BTCUSDT|47000.0,47500.0,48000.0";
        var priceFilePath = Path.Combine(_testDateFolder, _symbolPriceFileName);
        await File.WriteAllTextAsync(priceFilePath, testPriceContent);

        var service = new FileExchangeService(_testBasePath, _testDate, _symbolPriceFileName);
        await service.InitAsync();

        // Act
        var prices = await service.GetHistoricalPrices("NONEXISTENT", 1, PriceIntervalLetterEnum.D, 10);

        // Assert
        Assert.NotNull(prices);
        Assert.Empty(prices);
    }

    [Fact]
    public async Task InitAsync_WithNonExistentFolder_ShouldNotThrow()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testBasePath, "NonExistent");
        var service = new FileExchangeService(nonExistentPath, _testDate, _symbolPriceFileName);

        // Act & Assert - 不應該拋出異常
        await service.InitAsync();
        var tradingPairs = await service.GetTradingPairs();
        
        Assert.NotNull(tradingPairs);
        Assert.Empty(tradingPairs);
    }

    [Fact]
    public async Task InitAsync_WithMalformedPriceData_ShouldSkipInvalidLines()
    {
        // Arrange
        var testPriceContent = "BTCUSDT|47000.0,47500.0,48000.0\n" +
                              "INVALID_LINE_WITHOUT_SEPARATOR\n" +
                              "ETHUSDT|3000.0,3100.0,3200.0\n" +
                              "ANOTHER|INVALID|FORMAT";

        var priceFilePath = Path.Combine(_testDateFolder, _symbolPriceFileName);
        await File.WriteAllTextAsync(priceFilePath, testPriceContent);

        var service = new FileExchangeService(_testBasePath, _testDate, _symbolPriceFileName);

        // Act
        await service.InitAsync();
        var tradingPairs = await service.GetTradingPairs();

        // Assert
        Assert.NotNull(tradingPairs);
        Assert.Equal(2, tradingPairs.Count); // 只有兩個有效的行
        Assert.Contains("BTCUSDT", tradingPairs);
        Assert.Contains("ETHUSDT", tradingPairs);
    }

    [Fact]
    public void ConvertSymbolsToTradingViewFormat_ShouldReturnCorrectFormat()
    {
        // Arrange
        var service = new FileExchangeService(_testBasePath, _testDate, _symbolPriceFileName);
        var symbols = new List<string> { "BTCUSDT", "ETHUSDT", "ADAUSDT" };

        // Act
        var result = service.ConvertSymbolsToTradingViewFormat(symbols);

        // Assert
        Assert.Equal("BINANCE:BTCUSDT.P,BINANCE:ETHUSDT.P,BINANCE:ADAUSDT.P", result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, true);
        }
    }
}
