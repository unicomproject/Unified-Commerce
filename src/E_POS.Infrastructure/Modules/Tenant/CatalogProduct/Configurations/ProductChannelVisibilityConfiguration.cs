using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductChannelVisibilityConfiguration : IEntityTypeConfiguration<ProductChannelVisibility>
{
    public void Configure(EntityTypeBuilder<ProductChannelVisibility> builder)
    {
        builder.ToTable("product_channel_visibility");

        builder.HasKey(x => x.Id).HasName("pk_product_channel_visibility");

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

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id")
            .IsRequired(false);

        builder.Property(x => x.SalesChannelId)
            .HasColumnName("sales_channel_id")
            .IsRequired();

        builder.Property(x => x.IsVisible)
            .HasColumnName("is_visible")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.IsOrderable)
            .HasColumnName("is_orderable")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.AvailableFrom)
            .HasColumnName("available_from")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.AvailableUntil)
            .HasColumnName("available_until")
            .HasColumnType("timestamp with time zone")
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

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_channel_visibility_product_id_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_channel_visibility_product_variant_id_product_variants");

        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.SalesChannelId })
            .IsUnique()
            .HasDatabaseName("uq_product_channel_visibility_tenant_id_product_id_channel_id")
            .HasFilter("product_variant_id IS NULL");

        builder.HasIndex(x => new { x.TenantId, x.ProductVariantId, x.SalesChannelId })
            .IsUnique()
            .HasDatabaseName("uq_product_channel_visibility_tenant_id_variant_id_channel_id")
            .HasFilter("product_variant_id IS NOT NULL");

        builder.ToTable(t => t.HasCheckConstraint("ck_product_channel_visibility_available_dates", "available_until IS NULL OR available_from IS NULL OR available_until >= available_from"));
        builder.ToTable(t => t.HasCheckConstraint("ck_product_channel_visibility_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}


