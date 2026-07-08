using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductBarcodeConfiguration : IEntityTypeConfiguration<ProductBarcode>
{
    public void Configure(EntityTypeBuilder<ProductBarcode> builder)
    {
        builder.ToTable("product_barcodes");

        builder.HasKey(x => x.Id).HasName("pk_product_barcodes");

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

        builder.Property(x => x.Barcode)
            .HasColumnName("barcode")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.BarcodeType)
            .HasColumnName("barcode_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.UomId)
            .HasColumnName("uom_id")
            .IsRequired(false);

        builder.Property(x => x.QuantityPerScan)
            .HasColumnName("quantity_per_scan")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.IsPrimaryBarcode)
            .HasColumnName("is_primary_barcode")
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
            .HasConstraintName("fk_product_barcodes_product_id_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_barcodes_product_variant_id_product_variants");

        builder.HasIndex(x => new { x.TenantId, x.Barcode })
            .IsUnique()
            .HasDatabaseName("uq_product_barcodes_tenant_id_barcode");

        builder.ToTable(t => t.HasCheckConstraint("ck_product_barcodes_quantity_per_scan", "quantity_per_scan > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_product_barcodes_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}


