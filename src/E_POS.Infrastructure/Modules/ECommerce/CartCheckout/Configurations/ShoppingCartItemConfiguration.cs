using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Configurations;

public sealed class ShoppingCartItemConfiguration : IEntityTypeConfiguration<ShoppingCartItem>
{
    public void Configure(EntityTypeBuilder<ShoppingCartItem> builder)
    {
        builder.ToTable("shopping_cart_items");

        builder.HasKey(x => x.Id).HasName("pk_shopping_cart_items");

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

        builder.Property(x => x.ShoppingCartId)
            .HasColumnName("shopping_cart_id")
            .IsRequired();

        builder.Property(x => x.LineNumber)
            .HasColumnName("line_number")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id");

        builder.Property(x => x.SkuSnapshot)
            .HasColumnName("sku_snapshot")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100);

        builder.Property(x => x.ProductNameSnapshot)
            .HasColumnName("product_name_snapshot")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ProductStructure)
            .HasColumnName("product_structure")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnName("unit_price")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.LineSubtotalAmount)
            .HasColumnName("line_subtotal_amount")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.LineDiscountAmount)
            .HasColumnName("line_discount_amount")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.LineTaxAmount)
            .HasColumnName("line_tax_amount")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.LineTotalAmount)
            .HasColumnName("line_total_amount")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.LineStatus)
            .HasColumnName("line_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_shopping_cart_items_tenant_id_tenants");

        builder.HasOne<ShoppingCart>()
            .WithMany()
            .HasForeignKey(x => x.ShoppingCartId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_shopping_cart_items_shopping_cart_id_shopping_carts");

        builder.HasOne<E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_shopping_cart_items_product_id_products");

        builder.HasOne<E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_shopping_cart_items_product_variant_id_product_variants");

        builder.HasIndex(x => new { x.ShoppingCartId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("uq_shopping_cart_items_shopping_cart_id_line_number");

        builder.ToTable(t => {
            t.HasCheckConstraint("ck_shopping_cart_items_line_number", "line_number > 0");
            t.HasCheckConstraint("ck_shopping_cart_items_quantity", "quantity > 0");
            t.HasCheckConstraint("ck_shopping_cart_items_unit_price", "unit_price >= 0");
            t.HasCheckConstraint("ck_shopping_cart_items_line_subtotal_amount", "line_subtotal_amount >= 0");
            t.HasCheckConstraint("ck_shopping_cart_items_line_discount_amount", "line_discount_amount >= 0");
            t.HasCheckConstraint("ck_shopping_cart_items_line_tax_amount", "line_tax_amount >= 0");
            t.HasCheckConstraint("ck_shopping_cart_items_line_total_amount", "line_total_amount >= 0");
            t.HasCheckConstraint("ck_shopping_cart_items_line_status", "line_status IN ('ACTIVE', 'REMOVED', 'UNAVAILABLE', 'PRICE_CHANGED')");
        });
    }
}



