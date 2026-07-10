using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

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

        builder.Property(x => x.FeatureId)
            .HasColumnName("feature_id")
            .IsRequired();

        builder.Property(x => x.SourceType)
            .HasColumnName("source_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.SourceReferenceId)
            .HasColumnName("source_reference_id");

        builder.Property(x => x.IsEnabled)
            .HasColumnName("is_enabled");

        builder.Property(x => x.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.EffectiveUntil)
            .HasColumnName("effective_until")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RevokedByPlatformUserId)
            .HasColumnName("revoked_by_platform_user_id");

        builder.Property(x => x.RevokedReason)
            .HasColumnName("revoked_reason")
            .HasColumnType("text");

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id");

        builder.Property(x => x.UpdatedByPlatformUserId)
            .HasColumnName("updated_by_platform_user_id");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_feature_entitlements_tenant_id_tenants");

        builder.HasOne<PlatformFeature>()
            .WithMany()
            .HasForeignKey(x => x.PlatformFeatureId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_feature_entitlements_platform_feature_id_platform_features");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByPlatformUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tenant_feature_entitlements_created_by_platform_user_id_platform_users");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByPlatformUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tenant_feature_entitlements_updated_by_platform_user_id_platform_users");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.RevokedByPlatformUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tenant_feature_entitlements_revoked_by_platform_user_id_platform_users");

        builder.HasIndex(x => new { x.TenantId, x.PlatformFeatureId })
            .IsUnique()
            .HasDatabaseName("uq_tenant_feature_entitlements_tenant_id_platform_feature_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_tenant_feature_entitlements_entitlement_status", "entitlement_status IN ('ENABLED', 'DISABLED', 'EXPIRED')")); 
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_tenant_feature_entitlements_effective_dates",
            "effective_until IS NULL OR effective_until > effective_from"));
    }
}




