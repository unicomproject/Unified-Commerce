using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable("inventory_balances");

        builder.HasKey(x => x.Id).HasName("pk_inventory_balances");

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

        builder.Property(x => x.OnHandQuantity)
            .HasColumnName("on_hand_quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.ProductBatchId)
            .HasColumnName("product_batch_id");

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id");

        builder.Property(x => x.ReservedQuantity)
            .HasColumnName("reserved_quantity")
            .HasPrecision(18, 4);

        builder.HasOne<InventoryLocation>()
            .WithMany()
            .HasForeignKey(x => x.InventoryLocationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventory_balances_inventory_location_id_inventory_locations");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventory_balances_product_id_products");

        builder.HasIndex(x => new { x.InventoryLocationId, x.ProductId, x.ProductVariantId, x.ProductBatchId })
            .IsUnique()
            .HasDatabaseName("uq_inventory_balances_inventory_location_id_product_id_product_variant_id_product_batch_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_inventory_balances_on_hand_quantity", "on_hand_quantity >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_inventory_balances_reserved_quantity", "reserved_quantity >= 0")); 
    }
}

