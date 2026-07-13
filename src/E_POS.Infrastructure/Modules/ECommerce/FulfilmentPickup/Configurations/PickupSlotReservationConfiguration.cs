using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.FulfilmentPickup.Configurations;

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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.PickupSlotId)
            .HasColumnName("pickup_slot_id")
            .IsRequired();

        builder.Property(x => x.CheckoutSessionId)
            .HasColumnName("checkout_session_id")
            .IsRequired(false);

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired(false);

        builder.Property(x => x.ReservedCapacity)
            .HasColumnName("reserved_capacity")
            .IsRequired();

        builder.Property(x => x.ReservationStatus)
            .HasColumnName("reservation_status")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ConfirmedAt)
            .HasColumnName("confirmed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ReleasedAt)
            .HasColumnName("released_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ReleaseReason)
            .HasColumnName("release_reason")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250)
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_slot_reservations_2fb817a0");

        builder.HasOne<E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities.PickupSlot>()
            .WithMany()
            .HasForeignKey(x => x.PickupSlotId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_slot_reservations_04fa082b");

        builder.HasOne<E_POS.Domain.Modules.ECommerce.CartCheckout.Entities.CheckoutSession>()
            .WithMany()
            .HasForeignKey(x => x.CheckoutSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_slot_reservations_0ceb7dfa");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_slot_reservations_7b279b41");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_pickup_slot_reservations_reserved_capacity", "reserved_capacity > 0");
            t.HasCheckConstraint("ck_pickup_slot_reservations_session_or_order", "checkout_session_id IS NOT NULL OR sales_order_id IS NOT NULL");
            t.HasCheckConstraint("ck_pickup_slot_reservations_status", "reservation_status IN ('PENDING', 'CONFIRMED', 'RELEASED', 'EXPIRED', 'CANCELLED')");
        });
        // </second-brain-constraints>
    }
}

