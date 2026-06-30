using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.PricingTax.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.PricingTax.Configurations;

public sealed class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
    public void Configure(EntityTypeBuilder<PriceListItem> builder)
    {
        builder.ToTable("price_list_items");

        builder.HasKey(x => x.Id).HasName("pk_price_list_items");

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

        builder.Property(x => x.PriceAmount)
            .HasColumnName("price_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.PriceListId)
            .HasColumnName("price_list_id")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id")
            .IsRequired(false);

        builder.HasOne<PriceList>()
            .WithMany()
            .HasForeignKey(x => x.PriceListId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_price_list_items_price_list_id_price_lists");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_price_list_items_product_id_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_price_list_items_product_variant_id_product_variants");

        builder.HasIndex(x => new { x.PriceListId, x.ProductId, x.ProductVariantId })
            .IsUnique()
            .HasDatabaseName("uq_price_list_items_price_list_id_product_id_product_variant_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_price_list_items_price_amount", "price_amount >= 0")); 
    }
}

