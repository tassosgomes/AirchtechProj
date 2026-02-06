using Microsoft.EntityFrameworkCore;
using ModernizationPlatform.Infra.Persistence;

namespace ModernizationPlatform.API.IntegrationTests;

public class AppDbContextTests
{
    [Fact]
    public void DbSets_ShouldBeAvailable()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=postgres;Username=postgres;Password=postgres")
            .Options;

        using var context = new AppDbContext(options);

        Assert.NotNull(context.Users);
        Assert.NotNull(context.AnalysisRequests);
        Assert.NotNull(context.SharedContexts);
        Assert.NotNull(context.AnalysisJobs);
        Assert.NotNull(context.Findings);
        Assert.NotNull(context.Prompts);
        Assert.NotNull(context.Repositories);
    }
}
