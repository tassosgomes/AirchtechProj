using Moq;
using ModernizationPlatform.Application.Services;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;
using Xunit;

namespace ModernizationPlatform.API.UnitTests.Services;

public class PromptCatalogServiceTests
{
    private readonly Mock<IPromptRepository> _mockPromptRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly PromptCatalogService _service;

    public PromptCatalogServiceTests()
    {
        _mockPromptRepository = new Mock<IPromptRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _service = new PromptCatalogService(_mockPromptRepository.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPrompts()
    {
        // Arrange
        var prompts = new List<Prompt>
        {
            new Prompt(AnalysisType.Obsolescence, "Obsolescence prompt"),
            new Prompt(AnalysisType.Security, "Security prompt")
        };
        _mockPromptRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompts);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsPrompt()
    {
        // Arrange
        var promptId = Guid.NewGuid();
        var prompt = new Prompt(AnalysisType.Observability, "Observability prompt");
        _mockPromptRepository.Setup(r => r.GetByIdAsync(promptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);

        // Act
        var result = await _service.GetByIdAsync(promptId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AnalysisType.Observability, result.AnalysisType);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var promptId = Guid.NewGuid();
        _mockPromptRepository.Setup(r => r.GetByIdAsync(promptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Prompt?)null);

        // Act
        var result = await _service.GetByIdAsync(promptId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByAnalysisTypeAsync_ExistingType_ReturnsPrompt()
    {
        // Arrange
        var prompt = new Prompt(AnalysisType.Documentation, "Documentation prompt");
        _mockPromptRepository.Setup(r => r.GetByAnalysisTypeAsync(AnalysisType.Documentation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);

        // Act
        var result = await _service.GetByAnalysisTypeAsync(AnalysisType.Documentation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AnalysisType.Documentation, result.AnalysisType);
    }

    [Fact]
    public async Task GetByAnalysisTypeAsync_NonExistingType_ReturnsNull()
    {
        // Arrange
        _mockPromptRepository.Setup(r => r.GetByAnalysisTypeAsync(AnalysisType.Security, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Prompt?)null);

        // Act
        var result = await _service.GetByAnalysisTypeAsync(AnalysisType.Security);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_NewPrompt_CreatesPrompt()
    {
        // Arrange
        _mockPromptRepository.Setup(r => r.GetByAnalysisTypeAsync(AnalysisType.Security, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Prompt?)null);
        _mockPromptRepository.Setup(r => r.AddAsync(It.IsAny<Prompt>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateOrUpdateAsync(AnalysisType.Security, "New security prompt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AnalysisType.Security, result.AnalysisType);
        Assert.Equal("New security prompt", result.Content);
        _mockPromptRepository.Verify(r => r.AddAsync(It.IsAny<Prompt>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_ExistingPrompt_UpdatesPrompt()
    {
        // Arrange
        var existingPrompt = new Prompt(AnalysisType.Obsolescence, "Old content");
        _mockPromptRepository.Setup(r => r.GetByAnalysisTypeAsync(AnalysisType.Obsolescence, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPrompt);
        _mockPromptRepository.Setup(r => r.Update(It.IsAny<Prompt>()));
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateOrUpdateAsync(AnalysisType.Obsolescence, "Updated content");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated content", result.Content);
        _mockPromptRepository.Verify(r => r.Update(It.IsAny<Prompt>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_EmptyContent_ThrowsArgumentException()
    {
        // Arrange
        _mockPromptRepository.Setup(r => r.GetByAnalysisTypeAsync(AnalysisType.Security, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Prompt?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateOrUpdateAsync(AnalysisType.Security, ""));
    }
}
