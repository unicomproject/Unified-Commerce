using E_POS.Domain.Modules.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class ProductVariantOptionValueConfiguration : IEntityTypeConfiguration<ProductVariantOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductVariantOptionValue> builder)
    {
        builder.ToTable("product_variant_option_values");

        builder.HasKey(x => x.Id).HasName("pk_product_variant_option_values");

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

        builder.Property(x => x.ProductOptionValueId)
            .HasColumnName("product_option_value_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id")
            .IsRequired(false);

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_variant_option_values_product_variant_id_product_variants");

        builder.HasOne<ProductOptionValue>()
            .WithMany()
            .HasForeignKey(x => x.ProductOptionValueId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_variant_option_values_product_option_value_id_product_option_values");

        builder.HasIndex(x => new { x.ProductVariantId, x.ProductOptionValueId })
            .IsUnique()
            .HasDatabaseName("uq_product_variant_option_values_product_variant_id_product_option_value_id");
    }
}

