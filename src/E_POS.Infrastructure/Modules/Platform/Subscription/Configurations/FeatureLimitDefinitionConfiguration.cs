using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class FeatureLimitDefinitionConfiguration : IEntityTypeConfiguration<FeatureLimitDefinition>
{
    public void Configure(EntityTypeBuilder<FeatureLimitDefinition> builder)
    {
        builder.ToTable("feature_limit_definitions");

        builder.HasKey(x => x.Id).HasName("pk_feature_limit_definitions");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.LimitCode)
            .HasColumnName("limit_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DefaultLimitValue)
            .HasColumnName("default_limit_value")
            .HasPrecision(18, 4);

        builder.Property(x => x.PlatformFeatureId)
            .HasColumnName("platform_feature_id")
            .IsRequired();

        builder.Property(x => x.LimitKey)
            .HasColumnName("limit_key")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.LimitName)
            .HasColumnName("limit_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.ValueType)
            .HasColumnName("value_type")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.UnitCode)
            .HasColumnName("unit_code")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired(false);

        builder.Property(x => x.IsHardLimit)
            .HasColumnName("is_hard_limit")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.HasOne<PlatformFeature>()
            .WithMany()
            .HasForeignKey(x => x.PlatformFeatureId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_feature_limit_definitions_platform_feature_id_platform_features");

        builder.HasIndex(x => new { x.PlatformFeatureId, x.LimitCode })
            .IsUnique()
            .HasDatabaseName("uq_feature_limit_definitions_platform_feature_id_limit_code");

        builder.HasIndex(x => new { x.PlatformFeatureId, x.LimitKey })
            .IsUnique()
            .HasDatabaseName("uq_feature_limit_definitions_platform_feature_id_limit_key");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_feature_limit_definitions_default_limit_value",
            "default_limit_value IS NULL OR default_limit_value >= 0"));
    }
}
