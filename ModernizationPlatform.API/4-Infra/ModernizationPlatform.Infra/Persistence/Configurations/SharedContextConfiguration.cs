using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.Infra.Persistence.Configurations;

public class SharedContextConfiguration : IEntityTypeConfiguration<SharedContext>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<SharedContext> builder)
    {
        var stringListConverter = new ValueConverter<List<string>, string>(
            list => JsonSerializer.Serialize(list, JsonOptions),
            json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>());
        var stringListComparer = new ValueComparer<List<string>>(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            list => list.ToList());

        builder.ToTable("shared_contexts");

        builder.HasKey(context => context.Id);

        builder.Property(context => context.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(context => context.RequestId)
            .HasColumnName("request_id")
            .IsRequired();

        builder.Property(context => context.Version)
            .HasColumnName("version")
            .IsRequired();

        var languagesProperty = builder.Property(context => context.Languages);
        languagesProperty
            .HasColumnName("languages")
            .HasColumnType("jsonb")
            .HasConversion(stringListConverter);
        languagesProperty.Metadata.SetValueComparer(stringListComparer);
        languagesProperty.IsRequired();

        var frameworksProperty = builder.Property(context => context.Frameworks);
        frameworksProperty
            .HasColumnName("frameworks")
            .HasColumnType("jsonb")
            .HasConversion(stringListConverter);
        frameworksProperty.Metadata.SetValueComparer(stringListComparer);
        frameworksProperty.IsRequired();

        var dependenciesProperty = builder.Property(context => context.Dependencies);
        dependenciesProperty
            .HasColumnName("dependencies")
            .HasColumnType("jsonb")
            .HasConversion(stringListConverter);
        dependenciesProperty.Metadata.SetValueComparer(stringListComparer);
        dependenciesProperty.IsRequired();

        builder.Property(context => context.DirectoryStructureJson)
            .HasColumnName("directory_structure")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(context => context.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne<AnalysisRequest>()
            .WithMany()
            .HasForeignKey(context => context.RequestId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_shared_contexts_analysis_requests");

        builder.HasIndex(context => context.RequestId)
            .HasDatabaseName("IX_shared_contexts_request_id");
    }
}