using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class StockMovementCostAllocationConfiguration : IEntityTypeConfiguration<StockMovementCostAllocation>
{
    public void Configure(EntityTypeBuilder<StockMovementCostAllocation> builder)
    {
        builder.ToTable("stock_movement_cost_allocations");

        builder.HasKey(x => x.Id).HasName("pk_stock_movement_cost_allocations");

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

        builder.Property(x => x.AllocatedCostAmount)
            .HasColumnName("allocated_cost_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.AllocatedQuantity)
            .HasColumnName("allocated_quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.InventoryCostLayerId)
            .HasColumnName("inventory_cost_layer_id")
            .IsRequired();

        builder.Property(x => x.StockMovementId)
            .HasColumnName("stock_movement_id")
            .IsRequired();

        builder.HasOne<StockMovement>()
            .WithMany()
            .HasForeignKey(x => x.StockMovementId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stock_movement_cost_allocations_stock_movement_id_stock_movements");

        builder.HasOne<InventoryCostLayer>()
            .WithMany()
            .HasForeignKey(x => x.InventoryCostLayerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stock_movement_cost_allocations_inventory_cost_layer_id_inventory_cost_layers");

        builder.ToTable(t => t.HasCheckConstraint("ck_stock_movement_cost_allocations_allocated_quantity", "allocated_quantity > 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_stock_movement_cost_allocations_allocated_cost_amount", "allocated_cost_amount >= 0")); 
    }
}

