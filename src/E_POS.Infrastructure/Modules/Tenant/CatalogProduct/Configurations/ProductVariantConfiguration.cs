using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");

        builder.HasKey(x => x.Id).HasName("pk_product_variants");

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

        builder.Property(x => x.VariantCode)
            .HasColumnName("variant_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.Sku)
            .HasColumnName("sku")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.VariantName)
            .HasColumnName("variant_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.StockUomId)
            .HasColumnName("stock_uom_id")
            .IsRequired();

        builder.Property(x => x.SalesUomId)
            .HasColumnName("sales_uom_id")
            .IsRequired();

        builder.Property(x => x.OptionCombinationHash)
            .HasColumnName("option_combination_hash")
            .HasColumnType("char(64)")
            .HasMaxLength(64)
            .IsRequired(false);

        builder.Property(x => x.IsDefaultVariant)
            .HasColumnName("is_default_variant")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsSellable)
            .HasColumnName("is_sellable")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.AllowFractionalQuantity)
            .HasColumnName("allow_fractional_quantity")
            .HasDefaultValue(false)
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

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_variants_product_id_products");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.StockUomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_variants_stock_uom_id_unit_of_measures");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.SalesUomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_variants_sales_uom_id_unit_of_measures");

        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.VariantCode })
            .IsUnique()
            .HasDatabaseName("uq_product_variants_tenant_id_product_id_variant_code");

        builder.HasIndex(x => new { x.TenantId, x.Sku })
            .IsUnique()
            .HasDatabaseName("uq_product_variants_tenant_id_sku")
            .HasFilter("sku IS NOT NULL");

        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.OptionCombinationHash })
            .IsUnique()
            .HasDatabaseName("uq_product_variants_tenant_id_product_id_option_combination_hash")
            .HasFilter("option_combination_hash IS NOT NULL");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_product_variants_tenant_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_product_variants_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}


