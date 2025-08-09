using Microsoft.Extensions.Configuration;
using RSDailyFilter;
using System.Reflection;

namespace RSDailyFilter.Tests;

/// <summary>
/// Program 類別測試 - 主要測試程式入口點和基本流程
/// 注意：計算邏輯已移至 RsAnalysisService，相關測試請參考對應的服務測試
/// </summary>
public class ProgramCalculationTests
{
    [Fact]
    public void Program_ShouldBePublicClass()
    {
        // Arrange & Act
        var programType = typeof(RSDailyFilter.Program);

        // Assert
        Assert.True(programType.IsPublic, "Program 類別應該是 public");
    }

    [Fact]
    public void Program_ShouldHaveMainMethod()
    {
        // Arrange
        var programType = typeof(RSDailyFilter.Program);

        // Act - 列出所有方法來調試
        var allMethods = programType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        var methodNames = string.Join(", ", allMethods.Select(m => $"{m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name))})"));
        
        // 查找 Main 方法
        var mainMethod = allMethods.FirstOrDefault(m => m.Name == "Main");

        // Assert
        Assert.NotNull(mainMethod);
        Assert.True(mainMethod.IsStatic, "Main 方法應該是 static");
    }
    
    [Fact]
    public void Program_ShouldHaveDeleteOldResultFoldersMethod()
    {
        // Arrange
        var programType = typeof(RSDailyFilter.Program);

        // Act
        var method = programType.GetMethod("DeleteOldResultFolders", BindingFlags.NonPublic | BindingFlags.Static);

        // Assert
        Assert.NotNull(method);
        Assert.True(method.IsStatic, "DeleteOldResultFolders 方法應該是 static");
    }
}
