using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.Infra.Persistence.Configurations;

public class AnalysisJobConfiguration : IEntityTypeConfiguration<AnalysisJob>
{
    public void Configure(EntityTypeBuilder<AnalysisJob> builder)
    {
        var durationConverter = new ValueConverter<TimeSpan?, long?>(
            duration => duration.HasValue ? (long?)duration.Value.TotalMilliseconds : null,
            value => value.HasValue ? TimeSpan.FromMilliseconds(value.Value) : null);

        builder.ToTable("analysis_jobs");

        builder.HasKey(job => job.Id);

        builder.Property(job => job.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(job => job.RequestId)
            .HasColumnName("request_id")
            .IsRequired();

        builder.Property(job => job.Type)
            .HasColumnName("analysis_type")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(job => job.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(job => job.OutputJson)
            .HasColumnName("output")
            .HasColumnType("jsonb");

        builder.Property(job => job.Duration)
            .HasColumnName("duration_ms")
            .HasConversion(durationConverter);

        builder.Property(job => job.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne<AnalysisRequest>()
            .WithMany()
            .HasForeignKey(job => job.RequestId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_analysis_jobs_analysis_requests");

        builder.HasIndex(job => job.RequestId)
            .HasDatabaseName("IX_analysis_jobs_request_id");

        builder.HasIndex(job => job.Status)
            .HasDatabaseName("IX_analysis_jobs_status");
    }
}