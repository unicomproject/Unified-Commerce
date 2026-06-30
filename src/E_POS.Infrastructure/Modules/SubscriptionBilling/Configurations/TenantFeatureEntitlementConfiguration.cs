using E_POS.Domain.Modules.SubscriptionBilling.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.SubscriptionBilling.Configurations;

public sealed class TenantFeatureEntitlementConfiguration : IEntityTypeConfiguration<TenantFeatureEntitlement>
{
    public void Configure(EntityTypeBuilder<TenantFeatureEntitlement> builder)
    {
        builder.ToTable("tenant_feature_entitlements");

        builder.HasKey(x => x.Id).HasName("pk_tenant_feature_entitlements");

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

        builder.Property(x => x.EntitlementStatus)
            .HasColumnName("entitlement_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.PlatformFeatureId)
            .HasColumnName("platform_feature_id")
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_feature_entitlements_tenant_id_tenants");

        builder.HasOne<PlatformFeature>()
            .WithMany()
            .HasForeignKey(x => x.PlatformFeatureId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_feature_entitlements_platform_feature_id_platform_features");

        builder.HasIndex(x => new { x.TenantId, x.PlatformFeatureId })
            .IsUnique()
            .HasDatabaseName("uq_tenant_feature_entitlements_tenant_id_platform_feature_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_tenant_feature_entitlements_entitlement_status", "entitlement_status IN ('ENABLED', 'DISABLED', 'EXPIRED')")); 
    }
}

