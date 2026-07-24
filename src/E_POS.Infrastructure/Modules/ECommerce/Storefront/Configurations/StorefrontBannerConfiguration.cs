using E_POS.Domain.Modules.ECommerce.Storefront.Entities;
using E_POS.Domain.Modules.Shared.Media.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Configurations;

public class StorefrontBannerConfiguration : IEntityTypeConfiguration<StorefrontBanner>
{
    public void Configure(EntityTypeBuilder<StorefrontBanner> builder)
    {
        builder.ToTable("storefront_banners");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.SalesChannelId)
            .HasColumnName("sales_channel_id");

        builder.Property(x => x.BannerType)
            .HasColumnName("banner_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Subtitle)
            .HasColumnName("subtitle");

        builder.Property(x => x.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ImageMediaAssetId)
            .HasColumnName("image_media_asset_id")
            .IsRequired(false);

        builder.Property(x => x.ActionText)
            .HasColumnName("action_text")
            .HasMaxLength(50);

        builder.Property(x => x.ActionUrl)
            .HasColumnName("action_url")
            .HasMaxLength(500);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .IsRequired();

        // Auditable Entity mapping
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // Constraints and indexes
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_storefront_banners_tenant_id");
        builder.HasIndex(x => x.SalesChannelId).HasDatabaseName("ix_storefront_banners_sales_channel_id");
        builder.HasIndex(x => new { x.TenantId, x.ImageMediaAssetId })
            .HasDatabaseName("ix_storefront_banners_tenant_id_image_media_asset_id");
        
        // Tenant FK
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_storefront_banners_tenant_id_tenants")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ImageMediaAssetId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .HasConstraintName("fk_storefront_banners_image_media_asset_tenant")
            .OnDelete(DeleteBehavior.Restrict);

        // SalesChannel FK
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>()
            .WithMany()
            .HasForeignKey(x => x.SalesChannelId)
            .HasConstraintName("fk_storefront_banners_sales_channel_id_sales_channels")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
