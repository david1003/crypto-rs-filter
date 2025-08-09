using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using RSDailyFilter.ExchangeService;
using System.Net;

namespace RSDailyFilter.Tests.ExchangeService;

public class BinanceExchangeServiceTests : IDisposable
{
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private HttpClient _httpClient;
    private BinanceExchangeService _service;

    public BinanceExchangeServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _service = new BinanceExchangeService(_httpClient);
    }

    [Fact]
    public async Task GetTradingPairs_ShouldReturnUsdtPerpetualContracts()
    {
        // Arrange
        var mockExchangeInfo = new
        {
            symbols = new[]
            {
                new { symbol = "BTCUSDT", quoteAsset = "USDT", contractType = "PERPETUAL", status = "TRADING" },
                new { symbol = "ETHUSDT", quoteAsset = "USDT", contractType = "PERPETUAL", status = "TRADING" },
                new { symbol = "ADAUSDT", quoteAsset = "USDT", contractType = "PERPETUAL", status = "BREAK" }, // 非交易狀態
                new { symbol = "BTCBUSD", quoteAsset = "BUSD", contractType = "PERPETUAL", status = "TRADING" } // 非USDT
            }
        };

        var jsonResponse = JsonConvert.SerializeObject(mockExchangeInfo);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _service.GetTradingPairs();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("BTCUSDT", result);
        Assert.Contains("ETHUSDT", result);
        Assert.DoesNotContain("ADAUSDT", result); // 非交易狀態
        Assert.DoesNotContain("BTCBUSD", result); // 非USDT
    }

    [Fact]
    public async Task GetHistoricalPrices_ShouldReturnClosePrices()
    {
        // Arrange
        var mockKlineData = new[]
        {
            new object[] { 1640995200000, "47000.0", "48000.0", "46000.0", "47500.0", "100.0" },
            new object[] { 1641081600000, "47500.0", "49000.0", "47000.0", "48000.0", "150.0" },
            new object[] { 1641168000000, "48000.0", "49500.0", "47500.0", "48500.0", "120.0" }
        };

        var jsonResponse = JsonConvert.SerializeObject(mockKlineData);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _service.GetHistoricalPrices("BTCUSDT", 1, PriceIntervalLetterEnum.D, 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(47500.0, result[0]); // 第一根K線的收盤價
        Assert.Equal(48000.0, result[1]); // 第二根K線的收盤價
        Assert.Equal(48500.0, result[2]); // 第三根K線的收盤價
    }

    [Fact]
    public void ConvertSymbolsToTradingViewFormat_ShouldReturnCorrectFormat()
    {
        // Arrange
        var symbols = new List<string> { "BTCUSDT", "ETHUSDT", "ADAUSDT" };

        // Act
        var result = _service.ConvertSymbolsToTradingViewFormat(symbols);

        // Assert
        Assert.Equal("BINANCE:BTCUSDT.P,BINANCE:ETHUSDT.P,BINANCE:ADAUSDT.P", result);
    }

    [Fact]
    public async Task GetTradingPairs_WhenHttpRequestFails_ShouldThrowException()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _service.GetTradingPairs());
    }

    [Fact]
    public async Task GetHistoricalPrices_WithInvalidSymbol_ShouldHandleGracefully()
    {
        // Arrange - 設置 mock 以返回錯誤響應或空資料
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Invalid symbol")
            });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            _service.GetHistoricalPrices("INVALID", 1, PriceIntervalLetterEnum.D, 10));
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
