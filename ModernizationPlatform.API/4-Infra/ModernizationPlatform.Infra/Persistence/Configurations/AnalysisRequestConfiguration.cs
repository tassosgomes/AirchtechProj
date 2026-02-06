using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Infra.Persistence.Configurations;

public class AnalysisRequestConfiguration : IEntityTypeConfiguration<AnalysisRequest>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<AnalysisRequest> builder)
    {
        var selectedTypesConverter = new ValueConverter<List<AnalysisType>, string>(
            types => JsonSerializer.Serialize(types, JsonOptions),
            json => JsonSerializer.Deserialize<List<AnalysisType>>(json, JsonOptions) ?? new List<AnalysisType>());
        var selectedTypesComparer = new ValueComparer<List<AnalysisType>>(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            list => list.ToList());

        builder.ToTable("analysis_requests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(request => request.RepositoryUrl)
            .HasColumnName("repository_url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(request => request.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(request => request.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        var selectedTypesProperty = builder.Property(request => request.SelectedTypes);
        selectedTypesProperty
            .HasColumnName("selected_types")
            .HasColumnType("jsonb")
            .HasConversion(selectedTypesConverter);
        selectedTypesProperty.Metadata.SetValueComparer(selectedTypesComparer);
        selectedTypesProperty.IsRequired();

        builder.Property(request => request.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired();

        builder.Property(request => request.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(request => request.CompletedAt)
            .HasColumnName("completed_at");

        builder.HasIndex(request => request.Status)
            .HasDatabaseName("IX_analysis_requests_status");

        builder.HasIndex(request => request.CreatedAt)
            .HasDatabaseName("IX_analysis_requests_created_at");
    }
}