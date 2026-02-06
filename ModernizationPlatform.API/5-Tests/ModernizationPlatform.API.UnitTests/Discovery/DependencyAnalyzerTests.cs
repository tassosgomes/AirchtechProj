using Microsoft.Extensions.Logging.Abstractions;
using ModernizationPlatform.Domain.Services;
using ModernizationPlatform.Infra.Discovery;
using Xunit;

namespace ModernizationPlatform.API.UnitTests.Discovery;

public class DependencyAnalyzerTests
{
    private readonly IDependencyAnalyzer _analyzer;

    public DependencyAnalyzerTests()
    {
        _analyzer = new DependencyAnalyzer(NullLogger<DependencyAnalyzer>.Instance);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _analyzer.AnalyzeDependenciesAsync(string.Empty));
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithPackageJson_ExtractsNpmDependencies()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var packageJsonContent = @"{
  ""name"": ""test-app"",
  ""version"": ""1.0.0"",
  ""dependencies"": {
    ""react"": ""^18.2.0"",
    ""axios"": ""^1.6.0""
  },
  ""devDependencies"": {
    ""typescript"": ""^5.3.0""
  }
}";

            File.WriteAllText(Path.Combine(tempDir, "package.json"), packageJsonContent);

            // Act
            var dependencies = await _analyzer.AnalyzeDependenciesAsync(tempDir);

            // Assert
            Assert.NotEmpty(dependencies);
            Assert.Contains(dependencies, d => d.Name == "react" && d.Type == "npm");
            Assert.Contains(dependencies, d => d.Name == "axios" && d.Type == "npm");
            Assert.Contains(dependencies, d => d.Name == "typescript" && d.Type == "npm");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithRequirementsTxt_ExtractsPipDependencies()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var requirementsContent = @"flask==2.3.0
requests>=2.31.0
pytest~=7.4.0
# This is a comment
pandas==2.0.0";

            File.WriteAllText(Path.Combine(tempDir, "requirements.txt"), requirementsContent);

            // Act
            var dependencies = await _analyzer.AnalyzeDependenciesAsync(tempDir);

            // Assert
            Assert.NotEmpty(dependencies);
            Assert.Contains(dependencies, d => d.Name == "flask" && d.Version == "2.3.0");
            Assert.Contains(dependencies, d => d.Name == "requests" && d.Version == "2.31.0");
            Assert.Contains(dependencies, d => d.Name == "pandas" && d.Version == "2.0.0");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
