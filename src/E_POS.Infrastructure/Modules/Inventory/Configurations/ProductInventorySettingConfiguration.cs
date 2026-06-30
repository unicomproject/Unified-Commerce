using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class ProductInventorySettingConfiguration : IEntityTypeConfiguration<ProductInventorySetting>
{
    public void Configure(EntityTypeBuilder<ProductInventorySetting> builder)
    {
        builder.ToTable("product_inventory_settings");

        builder.HasKey(x => x.Id).HasName("pk_product_inventory_settings");

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
            .HasColumnName("product_variant_id")
            .IsRequired(false);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_inventory_settings_product_id_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_inventory_settings_product_variant_id_product_variants");

        builder.HasIndex(x => new { x.ProductId, x.ProductVariantId })
            .IsUnique()
            .HasDatabaseName("uq_product_inventory_settings_product_id_product_variant_id");
    }
}

