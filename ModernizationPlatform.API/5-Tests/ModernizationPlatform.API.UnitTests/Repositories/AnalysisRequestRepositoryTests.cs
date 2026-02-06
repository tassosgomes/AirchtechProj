using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Infra.Persistence;
using ModernizationPlatform.Infra.Repositories;

namespace ModernizationPlatform.API.UnitTests.Repositories;

public class AnalysisRequestRepositoryTests
{
    [Fact]
    public async Task CountQueuedBeforeAsync_ShouldReturnRequestsCreatedEarlier()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AnalysisRequests_" + Guid.NewGuid())
            .Options;

        await using var context = new AppDbContext(options);
        var repository = new AnalysisRequestRepository(context);

        var first = new AnalysisRequest("https://example.com/one", SourceProvider.GitHub, [AnalysisType.Security]);
        await repository.AddAsync(first, CancellationToken.None);
        await context.SaveChangesAsync();

        await Task.Delay(5);
        var second = new AnalysisRequest("https://example.com/two", SourceProvider.GitHub, [AnalysisType.Security]);
        await repository.AddAsync(second, CancellationToken.None);
        await context.SaveChangesAsync();

        await Task.Delay(5);
        var third = new AnalysisRequest("https://example.com/three", SourceProvider.GitHub, [AnalysisType.Security]);
        await repository.AddAsync(third, CancellationToken.None);
        await context.SaveChangesAsync();

        var count = await repository.CountQueuedBeforeAsync(third.CreatedAt, CancellationToken.None);

        count.Should().Be(2);
    }
}
