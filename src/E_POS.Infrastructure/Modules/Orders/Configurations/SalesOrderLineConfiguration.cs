using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Orders.Entities;
using E_POS.Domain.Modules.PricingTax.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Orders.Configurations;

public sealed class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.ToTable("sales_order_lines");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_lines");

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

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired();

        builder.Property(x => x.LineNumber)
            .HasColumnName("line_number")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id");

        builder.Property(x => x.UomId)
            .HasColumnName("uom_id")
            .IsRequired();

        builder.Property(x => x.PriceListItemId)
            .HasColumnName("price_list_item_id");

        builder.Property(x => x.SkuSnapshot)
            .HasColumnName("sku_snapshot")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100);

        builder.Property(x => x.ProductNameSnapshot)
            .HasColumnName("product_name_snapshot")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.VariantNameSnapshot)
            .HasColumnName("variant_name_snapshot")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200);

        builder.Property(x => x.UomCodeSnapshot)
            .HasColumnName("uom_code_snapshot")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.UomNameSnapshot)
            .HasColumnName("uom_name_snapshot")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ProductTypeSnapshot)
            .HasColumnName("product_type_snapshot")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ProductStructureSnapshot)
            .HasColumnName("product_structure_snapshot")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.FulfilledQuantity)
            .HasColumnName("fulfilled_quantity")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.CancelledQuantity)
            .HasColumnName("cancelled_quantity")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.ReturnedQuantity)
            .HasColumnName("returned_quantity")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.OriginalUnitPrice)
            .HasColumnName("original_unit_price")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.LineSubtotalAmount)
            .HasColumnName("line_subtotal_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.LineDiscountAmount)
            .HasColumnName("line_discount_amount")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LineTaxAmount)
            .HasColumnName("line_tax_amount")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LineTotalAmount)
            .HasColumnName("line_total_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.LineStatus)
            .HasColumnName("line_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_lines_tenant_id_tenants");

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_lines_sales_order_id_sales_orders");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_lines_product_id_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_lines_product_variant_id_product_variants");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.UomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_lines_uom_id_unit_of_measures");

        builder.HasOne<PriceListItem>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.PriceListItemId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_lines_price_list_item_id_price_list_items");

        builder.HasIndex(x => new { x.SalesOrderId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("uq_sales_order_lines_sales_order_id_line_number");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_sales_order_lines_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_sales_order_lines_quantity", "quantity > 0");
            t.HasCheckConstraint("ck_sales_order_lines_fulfilled_quantity", "fulfilled_quantity >= 0");
            t.HasCheckConstraint("ck_sales_order_lines_cancelled_quantity", "cancelled_quantity >= 0");
            t.HasCheckConstraint("ck_sales_order_lines_returned_quantity", "returned_quantity >= 0");
            t.HasCheckConstraint("ck_sales_order_lines_original_unit_price", "original_unit_price >= 0");
            t.HasCheckConstraint("ck_sales_order_lines_unit_price", "unit_price >= 0");
            t.HasCheckConstraint("ck_sales_order_lines_line_subtotal_amount", "line_subtotal_amount >= 0");
            t.HasCheckConstraint("ck_sales_order_lines_line_discount_amount", "line_discount_amount >= 0");
            t.HasCheckConstraint("ck_sales_order_lines_line_tax_amount", "line_tax_amount >= 0");
            t.HasCheckConstraint("ck_sales_order_lines_line_total_amount", "line_total_amount >= 0");
            t.HasCheckConstraint("ck_sales_order_lines_line_status", "line_status IN ('ACTIVE', 'PARTIALLY_FULFILLED', 'FULFILLED', 'CANCELLED', 'RETURNED')");
        });
    }
}
