using Microsoft.Extensions.Logging.Abstractions;
using ModernizationPlatform.Domain.Services;
using ModernizationPlatform.Infra.Discovery;
using Xunit;

namespace ModernizationPlatform.API.UnitTests.Discovery;

public class DotNetProjectAnalyzerTests
{
    private readonly IDotNetProjectAnalyzer _analyzer;

    public DotNetProjectAnalyzerTests()
    {
        _analyzer = new DotNetProjectAnalyzer(NullLogger<DotNetProjectAnalyzer>.Instance);
    }

    [Fact]
    public async Task AnalyzeAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _analyzer.AnalyzeAsync(string.Empty));
    }

    [Fact]
    public async Task AnalyzeAsync_WithNonExistentPath_ThrowsDirectoryNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
            _analyzer.AnalyzeAsync("/non/existent/path"));
    }

    [Fact]
    public async Task AnalyzeAsync_WithCsprojFile_ExtractsFrameworkAndDependencies()
    {
        // Arrange: Create a temporary directory with a sample .csproj
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
    <PackageReference Include=""Serilog"" Version=""3.1.1"" />
  </ItemGroup>
</Project>";

            File.WriteAllText(Path.Combine(tempDir, "Test.csproj"), csprojContent);

            // Act
            var (frameworks, dependencies) = await _analyzer.AnalyzeAsync(tempDir);

            // Assert
            Assert.NotEmpty(frameworks);
            Assert.Contains(frameworks, f => f.Name == ".NET" && f.Version == "8.0");
            
            Assert.NotEmpty(dependencies);
            Assert.Contains(dependencies, d => d.Name == "Newtonsoft.Json" && d.Version == "13.0.3");
            Assert.Contains(dependencies, d => d.Name == "Serilog" && d.Version == "3.1.1");
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
