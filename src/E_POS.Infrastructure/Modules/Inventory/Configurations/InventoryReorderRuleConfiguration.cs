using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class InventoryReorderRuleConfiguration : IEntityTypeConfiguration<InventoryReorderRule>
{
    public void Configure(EntityTypeBuilder<InventoryReorderRule> builder)
    {
        builder.ToTable("inventory_reorder_rules");

        builder.HasKey(x => x.Id).HasName("pk_inventory_reorder_rules");

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

        builder.Property(x => x.InventoryLocationId)
            .HasColumnName("inventory_location_id")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id");

        builder.Property(x => x.ReorderPointQuantity)
            .HasColumnName("reorder_point_quantity")
            .HasPrecision(18, 4);

        builder.HasOne<InventoryLocation>()
            .WithMany()
            .HasForeignKey(x => x.InventoryLocationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventory_reorder_rules_inventory_location_id_inventory_locations");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventory_reorder_rules_product_id_products");

        builder.HasIndex(x => new { x.InventoryLocationId, x.ProductId, x.ProductVariantId })
            .IsUnique()
            .HasDatabaseName("uq_inventory_reorder_rules_inventory_location_id_product_id_product_variant_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_inventory_reorder_rules_reorder_point_quantity", "reorder_point_quantity >= 0")); 
    }
}

