using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Configurations;

public sealed class InventoryReservationAllocationConfiguration : IEntityTypeConfiguration<InventoryReservationAllocation>
{
    public void Configure(EntityTypeBuilder<InventoryReservationAllocation> builder)
    {
        builder.ToTable("inventory_reservation_allocations");

        builder.HasKey(x => x.Id).HasName("pk_inventory_reservation_allocations");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.InventoryReservationLineId).HasColumnName("inventory_reservation_line_id").IsRequired();
        builder.Property(x => x.InventoryBalanceId).HasColumnName("inventory_balance_id").IsRequired();
        builder.Property(x => x.SerialNumberId).HasColumnName("serial_number_id").IsRequired(false);
        builder.Property(x => x.AllocatedQuantity).HasColumnName("allocated_quantity").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ReleasedQuantity).HasColumnName("released_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.FulfilledQuantity).HasColumnName("fulfilled_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.AllocationStatus).HasColumnName("allocation_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.AllocatedAt).HasColumnName("allocated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ReleasedAt).HasColumnName("released_at").HasColumnType("timestamp with time zone").IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reservation_allocations_tenant_id_tenants");
        
        builder.HasOne<InventoryReservationLine>().WithMany().HasForeignKey(x => new { x.TenantId, x.InventoryReservationLineId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reservation_allocations_inventory_reservation_line_id_inventory_reservation_lines");
        builder.HasOne<InventoryBalance>().WithMany().HasForeignKey(x => new { x.TenantId, x.InventoryBalanceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reservation_allocations_inventory_balance_id_inventory_balances");
        builder.HasOne<SerialNumber>().WithMany().HasForeignKey(x => new { x.TenantId, x.SerialNumberId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reservation_allocations_serial_number_id_serial_numbers");

        builder.HasIndex(x => new { x.TenantId, x.InventoryReservationLineId, x.InventoryBalanceId }).IsUnique().HasDatabaseName("uq_inventory_reservation_allocations_balance").HasFilter("serial_number_id IS NULL");
        builder.HasIndex(x => new { x.TenantId, x.InventoryReservationLineId, x.SerialNumberId }).IsUnique().HasDatabaseName("uq_inventory_reservation_allocations_serial").HasFilter("serial_number_id IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_inventory_reservation_allocations_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_inventory_reservation_allocations_quantities", "allocated_quantity > 0 AND released_quantity >= 0 AND fulfilled_quantity >= 0 AND released_quantity + fulfilled_quantity <= allocated_quantity");
            t.HasCheckConstraint("ck_inventory_reservation_allocations_released_at", "released_at IS NULL OR released_at >= allocated_at");
            t.HasCheckConstraint("ck_inventory_reservation_allocations_serial_quantity", "serial_number_id IS NULL OR allocated_quantity = 1");
        });
    }
}


