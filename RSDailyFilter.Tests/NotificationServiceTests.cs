using Microsoft.Extensions.Configuration;
using Moq;
using RSDailyFilter.ExchangeService;
using RSDailyFilter.Helper;
using RSDailyFilter.Models;
using RSDailyFilter.Services;

namespace RSDailyFilter.Tests;

/// <summary>
/// 通知服務測試
/// </summary>
public class NotificationServiceTests
{
    [Fact]
    public void NotificationService_ShouldInitializeWithDependencies()
    {
        // Arrange
        var mockConfiguration = new Mock<IConfiguration>();
        var mockTelegramHelper = new Mock<TelegramHelper>("test-token");
        var mockExchangeService = new Mock<IExchangeService>();

        // Act
        var notificationService = new NotificationService(
            mockConfiguration.Object,
            mockTelegramHelper.Object,
            mockExchangeService.Object
        );

        // Assert
        Assert.NotNull(notificationService);
    }

    [Fact]
    public async Task SendContractUpdateNotificationAsync_WithMissingChatId_ShouldThrowException()
    {
        // Arrange
        var mockConfiguration = new Mock<IConfiguration>();
        var mockTelegramHelper = new Mock<TelegramHelper>("test-token");
        var mockExchangeService = new Mock<IExchangeService>();

        // 設定配置返回 null
        mockConfiguration.Setup(c => c["AppSettings:TgContractUpdateChatId"]).Returns((string?)null);

        var notificationService = new NotificationService(
            mockConfiguration.Object,
            mockTelegramHelper.Object,
            mockExchangeService.Object
        );

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => notificationService.SendContractUpdateNotificationAsync("test message")
        );
    }

    [Fact]
    public async Task SendRankingResultsAsync_WithMissingChatId_ShouldThrowException()
    {
        // Arrange
        var mockConfiguration = new Mock<IConfiguration>();
        var mockTelegramHelper = new Mock<TelegramHelper>("test-token");
        var mockExchangeService = new Mock<IExchangeService>();

        // 設定配置返回 null
        mockConfiguration.Setup(c => c["AppSettings:TgRsRankChatId"]).Returns((string?)null);

        var notificationService = new NotificationService(
            mockConfiguration.Object,
            mockTelegramHelper.Object,
            mockExchangeService.Object
        );

        var symbols = new List<SymbolStrength>
        {
            new SymbolStrength { Symbol = "BTCUSDT" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => notificationService.SendRankingResultsAsync(symbols, null)
        );
    }

    [Fact]
    public async Task SendRankingResultsAsync_WithNullRankedSymbols_ShouldReturnEarly()
    {
        // Arrange
        var mockConfiguration = new Mock<IConfiguration>();
        var mockTelegramHelper = new Mock<TelegramHelper>("test-token");
        var mockExchangeService = new Mock<IExchangeService>();

        // 設定基本配置值以避免異常
        mockConfiguration.Setup(c => c["AppSettings:TgRsRankChatId"]).Returns("test-chat-id");
        mockConfiguration.Setup(c => c["AppSettings:RSRankDailyResultPath"]).Returns("test-path");
        mockConfiguration.Setup(c => c["AppSettings:RSTakeCount"]).Returns("30");

        var notificationService = new NotificationService(
            mockConfiguration.Object,
            mockTelegramHelper.Object,
            mockExchangeService.Object
        );

        // Act - 這應該正常執行並輸出警告訊息
        await notificationService.SendRankingResultsAsync(null!, null);

        // Assert - 沒有例外被拋出即為成功
        Assert.True(true);
    }

    [Fact]
    public async Task SendRankingResultsAsync_WithEmptyRankedSymbols_ShouldReturnEarly()
    {
        // Arrange
        var mockConfiguration = new Mock<IConfiguration>();
        var mockTelegramHelper = new Mock<TelegramHelper>("test-token");
        var mockExchangeService = new Mock<IExchangeService>();

        // 設定基本配置值以避免異常
        mockConfiguration.Setup(c => c["AppSettings:TgRsRankChatId"]).Returns("test-chat-id");
        mockConfiguration.Setup(c => c["AppSettings:RSRankDailyResultPath"]).Returns("test-path");
        mockConfiguration.Setup(c => c["AppSettings:RSTakeCount"]).Returns("30");

        var notificationService = new NotificationService(
            mockConfiguration.Object,
            mockTelegramHelper.Object,
            mockExchangeService.Object
        );

        var emptySymbols = new List<SymbolStrength>();

        // Act - 這應該正常執行並輸出警告訊息
        await notificationService.SendRankingResultsAsync(emptySymbols, null);

        // Assert - 沒有例外被拋出即為成功
        Assert.True(true);
    }
}
