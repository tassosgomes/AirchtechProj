using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.Infra.Persistence.Configurations;

public class FindingConfiguration : IEntityTypeConfiguration<Finding>
{
    public void Configure(EntityTypeBuilder<Finding> builder)
    {
        builder.ToTable("findings");

        builder.HasKey(finding => finding.Id);

        builder.Property(finding => finding.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(finding => finding.JobId)
            .HasColumnName("job_id")
            .IsRequired();

        builder.Property(finding => finding.Severity)
            .HasColumnName("severity")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(finding => finding.Category)
            .HasColumnName("category")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(finding => finding.Title)
            .HasColumnName("title")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(finding => finding.Description)
            .HasColumnName("description")
            .IsRequired();

        builder.Property(finding => finding.FilePath)
            .HasColumnName("file_path")
            .HasMaxLength(1024)
            .IsRequired();

        builder.HasOne<AnalysisJob>()
            .WithMany()
            .HasForeignKey(finding => finding.JobId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_findings_analysis_jobs");

        builder.HasIndex(finding => finding.JobId)
            .HasDatabaseName("IX_findings_job_id");

        builder.HasIndex(finding => finding.Severity)
            .HasDatabaseName("IX_findings_severity");
    }
}