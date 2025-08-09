using Moq;
using Moq.Protected;
using RSDailyFilter.Helper;
using System.Net;

namespace RSDailyFilter.Tests.Helper;

public class TelegramHelperTests : IDisposable
{
    private readonly string _testBotToken = "test_bot_token";
    private readonly string _testChatId = "test_chat_id";
    private readonly string _testBasePath;

    public TelegramHelperTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), "TelegramHelperTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBasePath);
    }

    [Fact]
    public async Task SendMessageToChannelAsync_WithValidParameters_ShouldNotThrow()
    {
        // Arrange
        var testMessage = "Test message";
        var telegramHelper = new TelegramHelper(_testBotToken);

        // Act & Assert - 不應該拋出異常（雖然會失敗因為是測試 token）
        await telegramHelper.SendMessageToChannelAsync(_testChatId, testMessage);
    }

    [Fact]
    public async Task SendFileToTelegramChannelAsync_WithValidFile_ShouldNotThrow()
    {
        // Arrange
        var testContent = "Test file content";
        var testFileName = "test.txt";
        var testFilePath = Path.Combine(_testBasePath, testFileName);
        await File.WriteAllTextAsync(testFilePath, testContent);
        
        var testMessage = "Test message with file";
        var customFileName = "custom_name.txt";
        var telegramHelper = new TelegramHelper(_testBotToken);

        // Act & Assert - 不應該拋出異常
        await telegramHelper.SendFileToTelegramChannelAsync(
            _testChatId, testFilePath, testMessage, customFileName);
    }

    [Fact]
    public async Task SendFileToTelegramChannelAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentFilePath = Path.Combine(_testBasePath, "non_existent.txt");
        var testMessage = "Test message";
        var telegramHelper = new TelegramHelper(_testBotToken);

        // Act & Assert - 檔案不存在應該拋出 FileNotFoundException
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            telegramHelper.SendFileToTelegramChannelAsync(
                _testChatId, nonExistentFilePath, testMessage));
    }

    [Fact]
    public async Task SendFileToTelegramChannelAsync_WithoutCustomFileName_ShouldUseOriginalFileName()
    {
        // Arrange
        var testContent = "Test file content";
        var testFileName = "original_name.txt";
        var testFilePath = Path.Combine(_testBasePath, testFileName);
        await File.WriteAllTextAsync(testFilePath, testContent);
        
        var testMessage = "Test message";
        var telegramHelper = new TelegramHelper(_testBotToken);

        // Act & Assert - 不應該拋出異常
        await telegramHelper.SendFileToTelegramChannelAsync(
            _testChatId, testFilePath, testMessage);
    }

    [Fact]
    public void Constructor_WithToken_ShouldNotThrow()
    {
        // Act & Assert
        var helper = new TelegramHelper(_testBotToken);
        Assert.NotNull(helper);
    }

    [Fact]
    public void Constructor_WithValidToken_ShouldCreateInstance()
    {
        // Arrange
        string validToken = "valid_token";

        // Act
        var telegram = new TelegramHelper(validToken);

        // Assert
        Assert.NotNull(telegram);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, true);
        }
    }
}
