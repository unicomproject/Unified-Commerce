using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class InventoryReservationAllocationConfiguration : IEntityTypeConfiguration<InventoryReservationAllocation>
{
    public void Configure(EntityTypeBuilder<InventoryReservationAllocation> builder)
    {
        builder.ToTable("inventory_reservation_allocations");

        builder.HasKey(x => x.Id).HasName("pk_inventory_reservation_allocations");

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

        builder.Property(x => x.AllocatedQuantity)
            .HasColumnName("allocated_quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.InventoryBalanceId)
            .HasColumnName("inventory_balance_id")
            .IsRequired();

        builder.Property(x => x.InventoryReservationLineId)
            .HasColumnName("inventory_reservation_line_id")
            .IsRequired();

        builder.HasOne<InventoryReservationLine>()
            .WithMany()
            .HasForeignKey(x => x.InventoryReservationLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventory_reservation_allocations_inventory_reservation_line_id_inventory_reservation_lines");

        builder.HasOne<InventoryBalance>()
            .WithMany()
            .HasForeignKey(x => x.InventoryBalanceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventory_reservation_allocations_inventory_balance_id_inventory_balances");

        builder.ToTable(t => t.HasCheckConstraint("ck_inventory_reservation_allocations_allocated_quantity", "allocated_quantity > 0")); 
    }
}

