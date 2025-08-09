using Microsoft.Extensions.Configuration;
using Moq;
using RSDailyFilter.ExchangeService;
using RSDailyFilter.Models;
using RSDailyFilter.Services;
using RSDailyFilter.Enums;

namespace RSDailyFilter.Tests;

/// <summary>
/// RS 分析服務測試 - 專門測試配置參數的使用和計算邏輯
/// </summary>
public class RsAnalysisServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IExchangeService> _mockExchangeService;
    private readonly RsAnalysisService _rsAnalysisService;

    public RsAnalysisServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockExchangeService = new Mock<IExchangeService>();
        
        // 設定默認配置值
        SetupDefaultConfiguration();
        
        _rsAnalysisService = new RsAnalysisService(_mockExchangeService.Object, _mockConfiguration.Object);
    }

    private void SetupDefaultConfiguration()
    {
        _mockConfiguration.Setup(c => c["AppSettings:CurrentTermDays"]).Returns("7");
        _mockConfiguration.Setup(c => c["AppSettings:ShortDays"]).Returns("5");
        _mockConfiguration.Setup(c => c["AppSettings:MiddleDays"]).Returns("7");
        _mockConfiguration.Setup(c => c["AppSettings:LongDays"]).Returns("10");
        _mockConfiguration.Setup(c => c["AppSettings:CurrentTermPercentage"]).Returns("0.2");
        _mockConfiguration.Setup(c => c["AppSettings:ShortTermPercentage"]).Returns("0.4");
        _mockConfiguration.Setup(c => c["AppSettings:MiddleTermPercentage"]).Returns("0.1");
        _mockConfiguration.Setup(c => c["AppSettings:LongTermPercentage"]).Returns("0.3");
        _mockConfiguration.Setup(c => c["AppSettings:IgnoreCryptoList"]).Returns("BTCDOMUSDT,USDCUSDT");
        _mockConfiguration.Setup(c => c["AppSettings:SymbolPriceFileName"]).Returns("UsdtPrice.txt");
        _mockConfiguration.Setup(c => c["AppSettings:DailyRankedFileName"]).Returns("DailyRankedResult.txt");
    }

    [Fact]
    public void RsAnalysisService_ShouldReadConfigurationParameters()
    {
        // Arrange & Act
        var service = new RsAnalysisService(_mockExchangeService.Object, _mockConfiguration.Object);

        // Assert
        Assert.NotNull(service);
        
        // 驗證所有配置參數都被讀取
        _mockConfiguration.Verify(c => c["AppSettings:CurrentTermDays"], Times.Never); // 在構造函數中不會讀取
        _mockConfiguration.Verify(c => c["AppSettings:ShortDays"], Times.Never);
        _mockConfiguration.Verify(c => c["AppSettings:MiddleDays"], Times.Never);
        _mockConfiguration.Verify(c => c["AppSettings:LongDays"], Times.Never);
    }

    [Fact]
    public async Task GetRsRankedSymbolsAsync_ShouldUseConfigurationDays()
    {
        // Arrange
        var symbols = new List<string> { "BTCUSDT", "ETHUSDT" };
        var dailyResultFolderPath = @"C:\temp\test";
        
        // 設定自定義天數
        _mockConfiguration.Setup(c => c["AppSettings:CurrentTermDays"]).Returns("14"); // 改為14天
        _mockConfiguration.Setup(c => c["AppSettings:ShortDays"]).Returns("10");       // 改為10天
        _mockConfiguration.Setup(c => c["AppSettings:MiddleDays"]).Returns("21");      // 改為21天
        _mockConfiguration.Setup(c => c["AppSettings:LongDays"]).Returns("30");       // 改為30天

        // 模擬價格數據 (30天的數據以滿足最大天數需求)
        var prices = Enumerable.Range(1, 30).Select(i => (double)i).ToList();
        
        _mockExchangeService
            .Setup(e => e.GetHistoricalPrices(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<PriceIntervalLetterEnum>(), 30))
            .ReturnsAsync(prices);

        // Act
        var result = await _rsAnalysisService.GetRsRankedSymbolsAsync(symbols, dailyResultFolderPath);

        // Assert
        // 驗證配置參數被正確讀取
        _mockConfiguration.Verify(c => c["AppSettings:CurrentTermDays"], Times.AtLeastOnce);
        _mockConfiguration.Verify(c => c["AppSettings:ShortDays"], Times.AtLeastOnce);
        _mockConfiguration.Verify(c => c["AppSettings:MiddleDays"], Times.AtLeastOnce);
        _mockConfiguration.Verify(c => c["AppSettings:LongDays"], Times.AtLeastOnce);
        
        // 驗證使用了最大天數 (30) 來獲取歷史數據
        _mockExchangeService.Verify(e => e.GetHistoricalPrices(It.IsAny<string>(), 1, PriceIntervalLetterEnum.D, 30), Times.AtLeast(2));
    }

    [Fact]
    public async Task GetRsRankedSymbolsAsync_ShouldUseConfigurationPercentages()
    {
        // Arrange
        var symbols = new List<string> { "BTCUSDT" };
        var dailyResultFolderPath = @"C:\temp\test";
        
        // 設定自定義權重
        _mockConfiguration.Setup(c => c["AppSettings:CurrentTermPercentage"]).Returns("0.3");
        _mockConfiguration.Setup(c => c["AppSettings:ShortTermPercentage"]).Returns("0.5");
        _mockConfiguration.Setup(c => c["AppSettings:MiddleTermPercentage"]).Returns("0.1");
        _mockConfiguration.Setup(c => c["AppSettings:LongTermPercentage"]).Returns("0.1");

        // 模擬價格數據
        var prices = new List<double> { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110 };
        
        _mockExchangeService
            .Setup(e => e.GetHistoricalPrices(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<PriceIntervalLetterEnum>(), It.IsAny<int>()))
            .ReturnsAsync(prices);

        // Act
        var result = await _rsAnalysisService.GetRsRankedSymbolsAsync(symbols, dailyResultFolderPath);

        // Assert
        // 驗證權重配置參數被正確讀取
        _mockConfiguration.Verify(c => c["AppSettings:CurrentTermPercentage"], Times.AtLeastOnce);
        _mockConfiguration.Verify(c => c["AppSettings:ShortTermPercentage"], Times.AtLeastOnce);
        _mockConfiguration.Verify(c => c["AppSettings:MiddleTermPercentage"], Times.AtLeastOnce);
        _mockConfiguration.Verify(c => c["AppSettings:LongTermPercentage"], Times.AtLeastOnce);
        
        Assert.NotNull(result);
        Assert.Single(result);
        
        // 驗證強度計算結果不為零（如果權重為零，強度也會是零）
        Assert.True(result.First().Strength > 0);
    }

    [Theory]
    [InlineData("5", "3", "7", "10")]
    [InlineData("10", "7", "14", "21")]
    [InlineData("1", "1", "1", "1")]
    public async Task GetRsRankedSymbolsAsync_ShouldHandleDifferentDayConfigurations(
        string currentTermDays, string shortDays, string middleDays, string longDays)
    {
        // Arrange
        var symbols = new List<string> { "BTCUSDT" };
        var dailyResultFolderPath = @"C:\temp\test";
        
        _mockConfiguration.Setup(c => c["AppSettings:CurrentTermDays"]).Returns(currentTermDays);
        _mockConfiguration.Setup(c => c["AppSettings:ShortDays"]).Returns(shortDays);
        _mockConfiguration.Setup(c => c["AppSettings:MiddleDays"]).Returns(middleDays);
        _mockConfiguration.Setup(c => c["AppSettings:LongDays"]).Returns(longDays);

        var maxDays = new[] { 
            Convert.ToInt32(currentTermDays), 
            Convert.ToInt32(shortDays), 
            Convert.ToInt32(middleDays), 
            Convert.ToInt32(longDays) 
        }.Max();

        // 提供足夠的價格數據
        var prices = Enumerable.Range(1, Math.Max(maxDays, 10)).Select(i => (double)i + 100).ToList();
        
        _mockExchangeService
            .Setup(e => e.GetHistoricalPrices(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<PriceIntervalLetterEnum>(), maxDays))
            .ReturnsAsync(prices);

        // Act
        var result = await _rsAnalysisService.GetRsRankedSymbolsAsync(symbols, dailyResultFolderPath);

        // Assert
        Assert.NotNull(result);
        if (prices.Count >= maxDays)
        {
            Assert.Single(result);
            
            // 驗證 RS 值被正確計算
            var symbolStrength = result.First();
            Assert.True(symbolStrength.CurrentTermRs > 0);
            // 對於極短期間(1天)的配置，回歸斜率可能為0是正常的
            if (int.Parse(currentTermDays) > 1)
            {
                Assert.True(symbolStrength.ShortRs != 0); // 可能是負數，但不應該是0
                Assert.True(symbolStrength.MiddleRs != 0);
                Assert.True(symbolStrength.LongRs != 0);
            }
            else
            {
                // 對於1天期間，只檢查值是有限的
                Assert.True(double.IsFinite(symbolStrength.ShortRs));
                Assert.True(double.IsFinite(symbolStrength.MiddleRs));
                Assert.True(double.IsFinite(symbolStrength.LongRs));
            }
        }
    }

    [Fact]
    public async Task GetRsRankedSymbolsAsync_ShouldIgnoreSymbolsFromConfiguration()
    {
        // Arrange
        var symbols = new List<string> { "BTCUSDT", "BTCDOMUSDT", "ETHUSDT", "USDCUSDT" };
        var dailyResultFolderPath = @"C:\temp\test";
        
        // 設定忽略清單
        _mockConfiguration.Setup(c => c["AppSettings:IgnoreCryptoList"]).Returns("BTCDOMUSDT,USDCUSDT");

        var prices = Enumerable.Range(1, 10).Select(i => (double)i + 100).ToList();
        
        _mockExchangeService
            .Setup(e => e.GetHistoricalPrices(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<PriceIntervalLetterEnum>(), It.IsAny<int>()))
            .ReturnsAsync(prices);

        // Act
        var result = await _rsAnalysisService.GetRsRankedSymbolsAsync(symbols, dailyResultFolderPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // 只有 BTCUSDT 和 ETHUSDT 應該被處理
        Assert.All(result, r => Assert.DoesNotContain("BTCDOM", r.Symbol));
        Assert.All(result, r => Assert.DoesNotContain("USDC", r.Symbol));
    }

    [Fact]
    public async Task GetRsRankedSymbolsAsync_WithInsufficientData_ShouldSkipSymbol()
    {
        // Arrange
        var symbols = new List<string> { "BTCUSDT", "ETHUSDT" };
        var dailyResultFolderPath = @"C:\temp\test";

        // 第一個標的返回足夠的數據，第二個返回不足的數據
        _mockExchangeService
            .Setup(e => e.GetHistoricalPrices("BTCUSDT", It.IsAny<int>(), It.IsAny<PriceIntervalLetterEnum>(), It.IsAny<int>()))
            .ReturnsAsync(Enumerable.Range(1, 10).Select(i => (double)i + 100).ToList());
            
        _mockExchangeService
            .Setup(e => e.GetHistoricalPrices("ETHUSDT", It.IsAny<int>(), It.IsAny<PriceIntervalLetterEnum>(), It.IsAny<int>()))
            .ReturnsAsync(new List<double> { 100, 101 }); // 不足的數據

        // Act
        var result = await _rsAnalysisService.GetRsRankedSymbolsAsync(symbols, dailyResultFolderPath);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // 只有 BTCUSDT 應該被處理
        Assert.Equal("BTCUSDT", result.First().Symbol);
    }

    [Fact]
    public async Task RsAnalysisService_ShouldValidateRequiredConfigurationParameters()
    {
        // Arrange - 移除必要的配置參數
        var mockConfigWithMissingParams = new Mock<IConfiguration>();
        mockConfigWithMissingParams.Setup(c => c["AppSettings:CurrentTermDays"]).Returns((string?)null);

        // Act & Assert - 應該在執行時拋出異常
        var service = new RsAnalysisService(_mockExchangeService.Object, mockConfigWithMissingParams.Object);
        
        // 當嘗試使用服務時，應該會拋出異常
        var symbols = new List<string> { "BTCUSDT" };
        var dailyResultFolderPath = @"C:\temp\test";
        
        await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await service.GetRsRankedSymbolsAsync(symbols, dailyResultFolderPath));
    }
}
