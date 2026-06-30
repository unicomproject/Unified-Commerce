using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class ProductBatchConfiguration : IEntityTypeConfiguration<ProductBatch>
{
    public void Configure(EntityTypeBuilder<ProductBatch> builder)
    {
        builder.ToTable("product_batches");

        builder.HasKey(x => x.Id).HasName("pk_product_batches");

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
            .HasColumnName("tenant_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.BatchNumber)
            .HasColumnName("batch_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.ManufacturedDate)
            .HasColumnName("manufactured_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(x => x.ExpiryDate)
            .HasColumnName("expiry_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id")
            .IsRequired(false);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_batches_product_id_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_batches_product_variant_id_product_variants");

        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.ProductVariantId, x.BatchNumber })
            .IsUnique()
            .HasDatabaseName("uq_product_batches_tenant_id_product_id_product_variant_id_batch_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_product_batches_expiry_date_manufactured_date", "expiry_date IS NULL OR expiry_date >= manufactured_date")); 
    }
}

