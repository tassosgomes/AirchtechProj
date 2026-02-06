using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.Infra.Persistence.Configurations;

public class RepositoryConfiguration : IEntityTypeConfiguration<Repository>
{
    public void Configure(EntityTypeBuilder<Repository> builder)
    {
        builder.ToTable("repositories");

        builder.HasKey(repository => repository.Id);

        builder.Property(repository => repository.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(repository => repository.Url)
            .HasColumnName("url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(repository => repository.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(repository => repository.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(repository => repository.LastAnalysisAt)
            .HasColumnName("last_analysis_at");

        builder.HasIndex(repository => repository.Url)
            .IsUnique()
            .HasDatabaseName("IX_repositories_url");
    }
}