using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Shared.Media.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");

        builder.HasKey(x => x.Id).HasName("pk_brands");

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

        builder.Property(x => x.BrandCode)
            .HasColumnName("brand_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.BrandName)
            .HasColumnName("brand_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.BrandSlug)
            .HasColumnName("brand_slug")
            .HasColumnType("varchar(180)")
            .HasMaxLength(180)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.LogoUrl)
            .HasColumnName("logo_url")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.LogoMediaAssetId)
            .HasColumnName("logo_media_asset_id")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_brands_tenant_id_tenants");

        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.LogoMediaAssetId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_brands_logo_media_asset_tenant");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_brands_created_by_tenant_user_id_tenant_users");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_brands_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.BrandCode })
            .IsUnique()
            .HasDatabaseName("uq_brands_tenant_id_brand_code");

        builder.HasIndex(x => new { x.TenantId, x.BrandSlug })
            .IsUnique()
            .HasDatabaseName("uq_brands_tenant_id_brand_slug");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_brands_tenant_id_id");

        builder.HasIndex(x => new { x.TenantId, x.LogoMediaAssetId })
            .HasDatabaseName("ix_brands_tenant_id_logo_media_asset_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_brands_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')")); 
    }
}



