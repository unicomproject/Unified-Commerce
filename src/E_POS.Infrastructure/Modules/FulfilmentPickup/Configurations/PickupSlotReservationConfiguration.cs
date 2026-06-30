using E_POS.Domain.Modules.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.FulfilmentPickup.Configurations;

public sealed class PickupSlotReservationConfiguration : IEntityTypeConfiguration<PickupSlotReservation>
{
    public void Configure(EntityTypeBuilder<PickupSlotReservation> builder)
    {
        builder.ToTable("pickup_slot_reservations");

        builder.HasKey(x => x.Id).HasName("pk_pickup_slot_reservations");

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

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CheckoutSessionId)
            .HasColumnName("checkout_session_id");

        builder.Property(x => x.PickupSlotId)
            .HasColumnName("pickup_slot_id")
            .IsRequired();

        builder.Property(x => x.ReservedCapacity)
            .HasColumnName("reserved_capacity");

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id");

        builder.HasOne<PickupSlot>()
            .WithMany()
            .HasForeignKey(x => x.PickupSlotId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_slot_reservations_pickup_slot_id_pickup_slots");

        builder.ToTable(t => t.HasCheckConstraint("ck_pickup_slot_reservations_reserved_capacity", "reserved_capacity > 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_pickup_slot_reservations_checkout_session_id_sales_order_id", "checkout_session_id IS NOT NULL OR sales_order_id IS NOT NULL")); 
    }
}

