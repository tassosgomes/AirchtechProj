using Microsoft.Extensions.Logging.Abstractions;
using ModernizationPlatform.Domain.Services;
using ModernizationPlatform.Infra.Discovery;
using Xunit;

namespace ModernizationPlatform.API.UnitTests.Discovery;

public class LanguageDetectorServiceTests
{
    private readonly ILanguageDetectorService _service;

    public LanguageDetectorServiceTests()
    {
        _service = new LanguageDetectorService(NullLogger<LanguageDetectorService>.Instance);
    }

    [Fact]
    public async Task DetectLanguagesAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.DetectLanguagesAsync(string.Empty));
    }

    [Fact]
    public async Task DetectLanguagesAsync_WithNonExistentPath_ThrowsDirectoryNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
            _service.DetectLanguagesAsync("/non/existent/path"));
    }

    [Fact]
    public async Task DetectLanguagesAsync_WithTempDirectory_CanDetectFiles()
    {
        // Arrange: Create a temporary directory with sample files
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create test files
            File.WriteAllText(Path.Combine(tempDir, "test.cs"), "// C# file\nclass Test { }");
            File.WriteAllText(Path.Combine(tempDir, "test.js"), "// JS file\nconsole.log('test');");
            File.WriteAllText(Path.Combine(tempDir, "test.py"), "# Python file\nprint('test')");

            // Act
            var result = await _service.DetectLanguagesAsync(tempDir);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, l => l.Name == "C#");
            Assert.Contains(result, l => l.Name == "JavaScript");
            Assert.Contains(result, l => l.Name == "Python");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
