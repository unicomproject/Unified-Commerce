using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.PricingTax.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.PricingTax.Configurations;

public sealed class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
    public void Configure(EntityTypeBuilder<PriceListItem> builder)
    {
        builder.ToTable("price_list_items");

        builder.HasKey(x => x.Id).HasName("pk_price_list_items");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.PriceListId).HasColumnName("price_list_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.ProductVariantId).HasColumnName("product_variant_id").IsRequired(false);
        builder.Property(x => x.UomId).HasColumnName("uom_id").IsRequired(false);
        builder.Property(x => x.SellingPrice).HasColumnName("selling_price").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CompareAtPrice).HasColumnName("compare_at_price").HasPrecision(18, 4).IsRequired(false);
        builder.Property(x => x.MinQuantity).HasColumnName("min_quantity").HasPrecision(18, 4).HasDefaultValue(1m).IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnName("valid_from").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.ValidUntil).HasColumnName("valid_until").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_items_tenant_id_tenants");
        builder.HasOne<PriceList>().WithMany().HasForeignKey(x => x.PriceListId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_items_price_list_id_price_lists");
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_items_product_id_products");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_items_product_variant_id_product_variants");
        builder.HasOne<UnitOfMeasure>().WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_items_uom_id_unit_of_measures");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_items_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_items_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.PriceListId, x.ProductId, x.MinQuantity })
            .IsUnique()
            .HasDatabaseName("uq_price_list_items_product_scope_no_uom")
            .HasFilter("product_variant_id IS NULL AND uom_id IS NULL");

        builder.HasIndex(x => new { x.TenantId, x.PriceListId, x.ProductId, x.UomId, x.MinQuantity })
            .IsUnique()
            .HasDatabaseName("uq_price_list_items_product_scope_with_uom")
            .HasFilter("product_variant_id IS NULL AND uom_id IS NOT NULL");

        builder.HasIndex(x => new { x.TenantId, x.PriceListId, x.ProductVariantId, x.MinQuantity })
            .IsUnique()
            .HasDatabaseName("uq_price_list_items_variant_scope_no_uom")
            .HasFilter("product_variant_id IS NOT NULL AND uom_id IS NULL");

        builder.HasIndex(x => new { x.TenantId, x.PriceListId, x.ProductVariantId, x.UomId, x.MinQuantity })
            .IsUnique()
            .HasDatabaseName("uq_price_list_items_variant_scope_with_uom")
            .HasFilter("product_variant_id IS NOT NULL AND uom_id IS NOT NULL");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_price_list_items_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
            t.HasCheckConstraint("ck_price_list_items_selling_price", "selling_price >= 0");
            t.HasCheckConstraint("ck_price_list_items_compare_at_price", "compare_at_price IS NULL OR compare_at_price >= selling_price");
            t.HasCheckConstraint("ck_price_list_items_min_quantity", "min_quantity > 0");
            t.HasCheckConstraint("ck_price_list_items_valid_period", "valid_until IS NULL OR valid_from IS NULL OR valid_until >= valid_from");
        });
    }
}

