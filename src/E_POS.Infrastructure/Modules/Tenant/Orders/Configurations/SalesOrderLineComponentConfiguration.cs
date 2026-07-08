using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Orders.Configurations;

public sealed class SalesOrderLineComponentConfiguration : IEntityTypeConfiguration<SalesOrderLineComponent>
{
    public void Configure(EntityTypeBuilder<SalesOrderLineComponent> builder)
    {
        builder.ToTable("sales_order_line_components");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_line_components");

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

        builder.Property(x => x.SalesOrderLineId)
            .HasColumnName("sales_order_line_id")
            .IsRequired();

        builder.Property(x => x.ComboDefinitionId)
            .HasColumnName("combo_definition_id")
            .IsRequired();

        builder.Property(x => x.ComboComponentId)
            .HasColumnName("combo_component_id");

        builder.Property(x => x.ComboGroupItemId)
            .HasColumnName("combo_group_item_id");

        builder.Property(x => x.ItemSourceType)
            .HasColumnName("item_source_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ItemProductId)
            .HasColumnName("item_product_id")
            .IsRequired();

        builder.Property(x => x.ItemVariantId)
            .HasColumnName("item_variant_id");

        builder.Property(x => x.ItemUomId)
            .HasColumnName("item_uom_id")
            .IsRequired();

        builder.Property(x => x.ItemSkuSnapshot)
            .HasColumnName("item_sku_snapshot")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100);

        builder.Property(x => x.ItemNameSnapshot)
            .HasColumnName("item_name_snapshot")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ItemVariantNameSnapshot)
            .HasColumnName("item_variant_name_snapshot")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200);

        builder.Property(x => x.ItemUomCodeSnapshot)
            .HasColumnName("item_uom_code_snapshot")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ItemUomNameSnapshot)
            .HasColumnName("item_uom_name_snapshot")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.UnitPriceAdjustment)
            .HasColumnName("unit_price_adjustment")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.TotalPriceAdjustment)
            .HasColumnName("total_price_adjustment")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_components_tenant_id_tenants");

        builder.HasOne<SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderLineId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_components_sales_order_line_id_sales_order_lines");

        builder.HasOne<ComboDefinition>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ComboDefinitionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_components_combo_definition_id_combo_definitions");

        builder.HasOne<ComboComponent>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ComboComponentId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_components_combo_component_id_combo_components");

        builder.HasOne<ComboGroupItem>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ComboGroupItemId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_components_combo_group_item_id_combo_group_items");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ItemProductId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_components_item_product_id_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ItemVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_components_item_variant_id_product_variants");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.ItemUomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_components_item_uom_id_unit_of_measures");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_sales_order_line_components_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_sales_order_line_components_item_source_type", "item_source_type IN ('FIXED_COMPONENT', 'GROUP_ITEM')");
            t.HasCheckConstraint("ck_sales_order_line_components_quantity", "quantity > 0");
            t.HasCheckConstraint("ck_sales_order_line_components_sort_order", "sort_order >= 0");
            t.HasCheckConstraint("ck_sales_order_line_components_source_rules", "(item_source_type = 'FIXED_COMPONENT' AND combo_component_id IS NOT NULL AND combo_group_item_id IS NULL) OR (item_source_type = 'GROUP_ITEM' AND combo_group_item_id IS NOT NULL AND combo_component_id IS NULL)");
        });
    }
}



