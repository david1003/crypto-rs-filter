using RSDailyFilter.Common;

namespace RSDailyFilter.Tests.Common;

public class ToolsTests : IDisposable
{
    private readonly string _testBasePath;

    public ToolsTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), "RSDailyFilterTests", Guid.NewGuid().ToString());
    }

    [Fact]
    public async Task WriteTextToFile_ShouldCreateFileWithContent()
    {
        // Arrange
        var testContent = "Test content for file writing";
        var fileName = "test.txt";

        // Act
        var filePath = await Tools.WriteTextToFile(_testBasePath, fileName, testContent);

        // Assert
        Assert.True(File.Exists(filePath));
        var actualContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(testContent, actualContent);
        Assert.Equal(Path.Combine(_testBasePath, fileName), filePath);
    }

    [Fact]
    public async Task WriteTextToFile_WhenFolderDoesNotExist_ShouldCreateFolder()
    {
        // Arrange
        var testContent = "Test content";
        var fileName = "test.txt";
        var subFolder = Path.Combine(_testBasePath, "SubFolder");

        // Act
        var filePath = await Tools.WriteTextToFile(subFolder, fileName, testContent);

        // Assert
        Assert.True(Directory.Exists(subFolder));
        Assert.True(File.Exists(filePath));
        var actualContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(testContent, actualContent);
    }

    [Fact]
    public async Task WriteTextToFile_WhenFileExists_ShouldOverwriteFile()
    {
        // Arrange
        var fileName = "overwrite_test.txt";
        var initialContent = "Initial content";
        var newContent = "New content";

        // 先創建一個檔案
        var initialFilePath = await Tools.WriteTextToFile(_testBasePath, fileName, initialContent);
        Assert.Equal(initialContent, await File.ReadAllTextAsync(initialFilePath));

        // Act - 覆寫檔案
        var newFilePath = await Tools.WriteTextToFile(_testBasePath, fileName, newContent);

        // Assert
        Assert.Equal(initialFilePath, newFilePath);
        var actualContent = await File.ReadAllTextAsync(newFilePath);
        Assert.Equal(newContent, actualContent);
    }

    [Fact]
    public async Task WriteTextToFile_WithNullOrEmptyFolderPath_ShouldThrowArgumentException()
    {
        // Arrange
        var fileName = "test.txt";
        var content = "test content";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            Tools.WriteTextToFile(null, fileName, content));
        
        await Assert.ThrowsAsync<ArgumentException>(() => 
            Tools.WriteTextToFile("", fileName, content));
        
        await Assert.ThrowsAsync<ArgumentException>(() => 
            Tools.WriteTextToFile("   ", fileName, content));
    }

    [Fact]
    public async Task ReadFileContent_ShouldReturnFileContent()
    {
        // Arrange
        var testContent = "Test content for reading";
        var fileName = "read_test.txt";
        
        // 先創建檔案
        await Tools.WriteTextToFile(_testBasePath, fileName, testContent);

        // Act
        var actualContent = await Tools.ReadFileContent(_testBasePath, fileName);

        // Assert
        Assert.Equal(testContent, actualContent);
    }

    [Fact]
    public async Task ReadFileContent_WhenFileDoesNotExist_ShouldReturnEmptyString()
    {
        // Arrange
        var nonExistentFileName = "non_existent.txt";

        // Act
        var content = await Tools.ReadFileContent(_testBasePath, nonExistentFileName);

        // Assert
        Assert.Equal(string.Empty, content);
    }

    [Fact]
    public async Task ReadFileContent_WhenFolderDoesNotExist_ShouldReturnEmptyString()
    {
        // Arrange
        var nonExistentFolder = Path.Combine(_testBasePath, "NonExistent");
        var fileName = "test.txt";

        // Act
        var content = await Tools.ReadFileContent(nonExistentFolder, fileName);

        // Assert
        Assert.Equal(string.Empty, content);
    }

    [Fact]
    public async Task ReadFileContent_WithNullParameters_ShouldThrowArgumentNullException()
    {
        // Arrange
        var fileName = "test.txt";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            Tools.ReadFileContent(null!, fileName));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, true);
        }
    }
}
