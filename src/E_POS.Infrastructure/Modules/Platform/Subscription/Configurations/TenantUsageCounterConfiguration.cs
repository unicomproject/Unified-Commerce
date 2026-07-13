using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class TenantUsageCounterConfiguration : IEntityTypeConfiguration<TenantUsageCounter>
{
    public void Configure(EntityTypeBuilder<TenantUsageCounter> builder)
    {
        builder.ToTable("tenant_usage_counters");

        builder.HasKey(x => x.Id).HasName("pk_tenant_usage_counters");

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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.PlatformFeatureId)
            .HasColumnName("platform_feature_id")
            .IsRequired();

        builder.Property(x => x.UsagePeriodStart)
            .HasColumnName("usage_period_start")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.UsedQuantity)
            .HasColumnName("used_quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.FeatureLimitDefinitionId)
            .HasColumnName("feature_limit_definition_id")
            .IsRequired();

        builder.Property(x => x.UsageScope)
            .HasColumnName("usage_scope")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ScopeReferenceId)
            .HasColumnName("scope_reference_id");

        builder.Property(x => x.CurrentValue)
            .HasColumnName("current_value")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.LimitValue)
            .HasColumnName("limit_value")
            .HasPrecision(18, 4);

        builder.Property(x => x.PeriodStart)
            .HasColumnName("period_start")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.PeriodEnd)
            .HasColumnName("period_end")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.LastCalculatedAt)
            .HasColumnName("last_calculated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_usage_counters_tenant_id_tenants");

        builder.HasOne<PlatformFeature>()
            .WithMany()
            .HasForeignKey(x => x.PlatformFeatureId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_usage_counters_platform_feature_id_platform_features");

        builder.HasOne<FeatureLimitDefinition>()
            .WithMany()
            .HasForeignKey(x => x.FeatureLimitDefinitionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_usage_counters_feature_limit_definition_id_feature_limit_definitions");

        builder.HasIndex(x => x.FeatureLimitDefinitionId)
            .HasDatabaseName("ix_tenant_usage_counters_feature_limit_definition_id");

        builder.HasIndex(x => new { x.TenantId, x.PlatformFeatureId, x.UsagePeriodStart })
            .IsUnique()
            .HasDatabaseName("uq_tenant_usage_counters_tenant_id_platform_feature_id_usage_period_start");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_tenant_usage_counters_used_quantity", "used_quantity >= 0");
            t.HasCheckConstraint("ck_tenant_usage_counters_current_value", "current_value >= 0");
            t.HasCheckConstraint(
                "ck_tenant_usage_counters_limit_value",
                "limit_value IS NULL OR limit_value >= 0");
        });
    }
}
