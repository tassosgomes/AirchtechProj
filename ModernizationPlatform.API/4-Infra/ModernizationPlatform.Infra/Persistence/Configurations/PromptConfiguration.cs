using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.Infra.Persistence.Configurations;

public class PromptConfiguration : IEntityTypeConfiguration<Prompt>
{
    public void Configure(EntityTypeBuilder<Prompt> builder)
    {
        builder.ToTable("prompts");

        builder.HasKey(prompt => prompt.Id);

        builder.Property(prompt => prompt.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(prompt => prompt.AnalysisType)
            .HasColumnName("analysis_type")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(prompt => prompt.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(prompt => prompt.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(prompt => prompt.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(prompt => prompt.AnalysisType)
            .IsUnique()
            .HasDatabaseName("IX_prompts_analysis_type");
    }
}