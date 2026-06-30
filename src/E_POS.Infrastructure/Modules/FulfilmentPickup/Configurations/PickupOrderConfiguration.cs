using E_POS.Domain.Modules.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.FulfilmentPickup.Configurations;

public sealed class PickupOrderConfiguration : IEntityTypeConfiguration<PickupOrder>
{
    public void Configure(EntityTypeBuilder<PickupOrder> builder)
    {
        builder.ToTable("pickup_orders");

        builder.HasKey(x => x.Id).HasName("pk_pickup_orders");

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
            .HasColumnName("tenant_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.FulfillmentOrderId)
            .HasColumnName("fulfillment_order_id")
            .IsRequired();

        builder.Property(x => x.PickupNumber)
            .HasColumnName("pickup_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.PickupSlotReservationId)
            .HasColumnName("pickup_slot_reservation_id")
            .IsRequired();

        builder.HasOne<FulfillmentOrder>()
            .WithMany()
            .HasForeignKey(x => x.FulfillmentOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_orders_fulfillment_order_id_fulfillment_orders");

        builder.HasOne<PickupSlotReservation>()
            .WithMany()
            .HasForeignKey(x => x.PickupSlotReservationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_orders_pickup_slot_reservation_id_pickup_slot_reservations");

        builder.HasIndex(x => new { x.TenantId, x.PickupNumber })
            .IsUnique()
            .HasDatabaseName("uq_pickup_orders_tenant_id_pickup_number");
    }
}

