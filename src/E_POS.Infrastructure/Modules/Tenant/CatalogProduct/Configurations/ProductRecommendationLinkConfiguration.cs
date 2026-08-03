using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductRecommendationLinkConfiguration : IEntityTypeConfiguration<ProductRecommendationLink>
{
    public void Configure(EntityTypeBuilder<ProductRecommendationLink> builder)
    {
        builder.ToTable("product_recommendation_links");
        builder.HasKey(x => x.Id).HasName("pk_product_recommendation_links");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.SourceProductId).HasColumnName("source_product_id").IsRequired();
        builder.Property(x => x.SourceVariantId).HasColumnName("source_variant_id");
        builder.Property(x => x.RecommendedProductId).HasColumnName("recommended_product_id").IsRequired();
        builder.Property(x => x.RecommendedVariantId).HasColumnName("recommended_variant_id");
        builder.Property(x => x.RecommendationType).HasColumnName("recommendation_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id");
        builder.Property(x => x.SalesChannelId).HasColumnName("sales_channel_id");
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnName("valid_from").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ValidUntil).HasColumnName("valid_until").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id");
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_recommendation_links_tenant_id_tenants");
        builder.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.SourceProductId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_recommendation_links_source_product_tenant");
        builder.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.RecommendedProductId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_recommendation_links_recommended_product_tenant");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.TenantId, x.SourceProductId, x.SourceVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.ProductId, x.Id }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_recommendation_links_source_variant_product_tenant");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.TenantId, x.RecommendedProductId, x.RecommendedVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.ProductId, x.Id }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_recommendation_links_recommended_variant_product_tenant");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => new { x.TenantId, x.OutletId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_recommendation_links_outlet_tenant");
        builder.HasOne<SalesChannel>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesChannelId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_recommendation_links_sales_channel_tenant");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_recommendation_links_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_recommendation_links_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new
            {
                x.TenantId, x.SourceProductId, x.SourceVariantId,
                x.RecommendedProductId, x.RecommendedVariantId,
                x.RecommendationType, x.OutletId, x.SalesChannelId
            })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasFilter("status = 'ACTIVE'")
            .HasDatabaseName("uq_product_recommendation_links_active_relationship");

        builder.HasIndex(x => new { x.TenantId, x.SourceProductId, x.RecommendationType, x.Status, x.ValidFrom, x.ValidUntil })
            .HasDatabaseName("ix_product_recommendation_links_lookup");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique()
            .HasDatabaseName("uq_product_recommendation_links_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_product_recommendation_links_not_self", "source_product_id <> recommended_product_id");
            t.HasCheckConstraint("ck_product_recommendation_links_sort_order", "sort_order >= 0");
            t.HasCheckConstraint("ck_product_recommendation_links_valid_dates", "valid_until IS NULL OR valid_from IS NULL OR valid_until >= valid_from");
            t.HasCheckConstraint("ck_product_recommendation_links_type", "recommendation_type IN ('FREQUENTLY_BOUGHT_TOGETHER')");
            t.HasCheckConstraint("ck_product_recommendation_links_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        });
    }
}
