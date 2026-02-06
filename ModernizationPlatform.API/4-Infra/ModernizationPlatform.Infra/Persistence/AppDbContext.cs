using Microsoft.EntityFrameworkCore;
using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.Infra.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<AnalysisRequest> AnalysisRequests => Set<AnalysisRequest>();
    public DbSet<SharedContext> SharedContexts => Set<SharedContext>();
    public DbSet<AnalysisJob> AnalysisJobs => Set<AnalysisJob>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<Prompt> Prompts => Set<Prompt>();
    public DbSet<Repository> Repositories => Set<Repository>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}