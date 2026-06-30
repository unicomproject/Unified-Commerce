using E_POS.Domain.Modules.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class ProductAttributeValueOptionConfiguration : IEntityTypeConfiguration<ProductAttributeValueOption>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValueOption> builder)
    {
        builder.ToTable("product_attribute_value_options");

        builder.HasKey(x => x.Id).HasName("pk_product_attribute_value_options");

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

        builder.Property(x => x.AttributeOptionId)
            .HasColumnName("attribute_option_id")
            .IsRequired();

        builder.Property(x => x.ProductAttributeValueId)
            .HasColumnName("product_attribute_value_id")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne<ProductAttributeValue>()
            .WithMany()
            .HasForeignKey(x => x.ProductAttributeValueId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_attribute_value_options_product_attribute_value_id_product_attribute_values");

        builder.HasOne<ProductAttributeOption>()
            .WithMany()
            .HasForeignKey(x => x.AttributeOptionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_attribute_value_options_attribute_option_id_product_attribute_options");

        builder.HasIndex(x => new { x.ProductAttributeValueId, x.AttributeOptionId })
            .IsUnique()
            .HasDatabaseName("uq_product_attribute_value_options_product_attribute_value_id_attribute_option_id");
    }
}

