using E_POS.Domain.Modules.Inventory.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("inventory_reservations");

        builder.HasKey(x => x.Id).HasName("pk_inventory_reservations");

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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.ReservationStatus)
            .HasColumnName("reservation_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.ReservationNumber)
            .HasColumnName("reservation_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventory_reservations_tenant_id_tenants");

        builder.HasIndex(x => new { x.TenantId, x.ReservationNumber })
            .IsUnique()
            .HasDatabaseName("uq_inventory_reservations_tenant_id_reservation_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_inventory_reservations_reservation_status", "reservation_status IN ('PENDING', 'CONFIRMED', 'RELEASED', 'EXPIRED', 'CANCELLED')")); 
    }
}

