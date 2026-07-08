using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
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

        builder.HasIndex(x => new { x.TenantId, x.PlatformFeatureId, x.UsagePeriodStart })
            .IsUnique()
            .HasDatabaseName("uq_tenant_usage_counters_tenant_id_platform_feature_id_usage_period_start");

        builder.ToTable(t => t.HasCheckConstraint("ck_tenant_usage_counters_used_quantity", "used_quantity >= 0")); 
    }
}




