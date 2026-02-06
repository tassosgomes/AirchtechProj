using Microsoft.Extensions.Logging.Abstractions;
using ModernizationPlatform.Domain.Services;
using ModernizationPlatform.Infra.Discovery;
using Xunit;

namespace ModernizationPlatform.API.UnitTests.Discovery;

public class DirectoryStructureMapperTests
{
    private readonly IDirectoryStructureMapper _mapper;

    public DirectoryStructureMapperTests()
    {
        _mapper = new DirectoryStructureMapper(NullLogger<DirectoryStructureMapper>.Instance);
    }

    [Fact]
    public void MapStructure_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _mapper.MapStructure(string.Empty));
    }

    [Fact]
    public void MapStructure_WithNonExistentPath_ThrowsDirectoryNotFoundException()
    {
        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => _mapper.MapStructure("/non/existent/path"));
    }

    [Fact]
    public void MapStructure_WithValidPath_ReturnsStructure()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create test structure
            var srcDir = Path.Combine(tempDir, "src");
            Directory.CreateDirectory(srcDir);
            File.WriteAllText(Path.Combine(srcDir, "index.js"), "console.log('test');");
            File.WriteAllText(Path.Combine(tempDir, "README.md"), "# Test");

            // Act
            var result = _mapper.MapStructure(tempDir);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("directory", result.Type);
            Assert.NotEmpty(result.Children);
            
            // Should have src directory and README.md
            Assert.Contains(result.Children, n => n.Name == "src" && n.Type == "directory");
            Assert.Contains(result.Children, n => n.Name == "README.md" && n.Type == "file");
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
    public void MapStructure_ExcludesCommonDirectories()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create directories that should be excluded
            Directory.CreateDirectory(Path.Combine(tempDir, "node_modules"));
            Directory.CreateDirectory(Path.Combine(tempDir, "bin"));
            Directory.CreateDirectory(Path.Combine(tempDir, "obj"));
            Directory.CreateDirectory(Path.Combine(tempDir, ".git"));
            
            // Create directory that should be included
            Directory.CreateDirectory(Path.Combine(tempDir, "src"));

            // Act
            var result = _mapper.MapStructure(tempDir);

            // Assert
            Assert.DoesNotContain(result.Children, n => n.Name == "node_modules");
            Assert.DoesNotContain(result.Children, n => n.Name == "bin");
            Assert.DoesNotContain(result.Children, n => n.Name == "obj");
            Assert.DoesNotContain(result.Children, n => n.Name == ".git");
            Assert.Contains(result.Children, n => n.Name == "src");
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
