using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.PricingTax.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.PricingTax.Configurations;

public sealed class ProductTaxAssignmentConfiguration : IEntityTypeConfiguration<ProductTaxAssignment>
{
    public void Configure(EntityTypeBuilder<ProductTaxAssignment> builder)
    {
        builder.ToTable("product_tax_assignments");

        builder.HasKey(x => x.Id).HasName("pk_product_tax_assignments");

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

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id");

        builder.Property(x => x.TaxClassId)
            .HasColumnName("tax_class_id")
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_tax_assignments_product_id_products");

        builder.HasOne<TaxClass>()
            .WithMany()
            .HasForeignKey(x => x.TaxClassId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_tax_assignments_tax_class_id_tax_classes");

        builder.HasIndex(x => new { x.ProductId, x.ProductVariantId, x.TaxClassId })
            .IsUnique()
            .HasDatabaseName("uq_product_tax_assignments_product_id_product_variant_id_tax_class_id");
    }
}

