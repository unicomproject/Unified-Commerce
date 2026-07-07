using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ComboGroupItemConfiguration : IEntityTypeConfiguration<ComboGroupItem>
{
    public void Configure(EntityTypeBuilder<ComboGroupItem> builder)
    {
        builder.ToTable("combo_group_items");

        builder.HasKey(x => x.Id).HasName("pk_combo_group_items");

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

        builder.Property(x => x.ComboGroupId)
            .HasColumnName("combo_group_id")
            .IsRequired();

        builder.Property(x => x.ItemProductId)
            .HasColumnName("item_product_id")
            .IsRequired();

        builder.Property(x => x.ItemVariantId)
            .HasColumnName("item_variant_id")
            .IsRequired(false);

        builder.Property(x => x.ItemUomId)
            .HasColumnName("item_uom_id")
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.BasePriceAdjustment)
            .HasColumnName("base_price_adjustment")
            .HasColumnType("numeric(18,4)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.IsDefaultItem)
            .HasColumnName("is_default_item")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();

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

        builder.HasOne<ComboGroup>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ComboGroupId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_group_items_combo_group_id_combo_groups");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ItemProductId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_group_items_item_product_id_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ItemVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_group_items_item_variant_id_product_variants");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.ItemUomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_group_items_item_uom_id_unit_of_measures");

        builder.HasIndex(x => new { x.ComboGroupId, x.ItemProductId, x.ItemUomId })
            .IsUnique()
            .HasDatabaseName("uq_combo_group_items_combo_group_id_item_product_id_uom_id")
            .HasFilter("item_variant_id IS NULL");

        builder.HasIndex(x => new { x.ComboGroupId, x.ItemVariantId, x.ItemUomId })
            .IsUnique()
            .HasDatabaseName("uq_combo_group_items_combo_group_id_item_variant_id_uom_id")
            .HasFilter("item_variant_id IS NOT NULL");

        builder.ToTable(t => t.HasCheckConstraint("ck_combo_group_items_quantity", "quantity > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_combo_group_items_sort_order", "sort_order >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_combo_group_items_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}


