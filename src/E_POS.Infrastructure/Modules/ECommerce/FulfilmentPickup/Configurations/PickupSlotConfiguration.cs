using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.FulfilmentPickup.Configurations;

public sealed class PickupSlotConfiguration : IEntityTypeConfiguration<PickupSlot>
{
    public void Configure(EntityTypeBuilder<PickupSlot> builder)
    {
        builder.ToTable("pickup_slots");

        builder.HasKey(x => x.Id).HasName("pk_pickup_slots");

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

        builder.Property(x => x.FulfillmentMethodOutletId)
            .HasColumnName("fulfillment_method_outlet_id")
            .IsRequired();

        builder.Property(x => x.SlotCode)
            .HasColumnName("slot_code")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.SlotDate)
            .HasColumnName("slot_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.WindowStart)
            .HasColumnName("window_start")
            .HasColumnType("time")
            .IsRequired();

        builder.Property(x => x.WindowEnd)
            .HasColumnName("window_end")
            .HasColumnType("time")
            .IsRequired();

        builder.Property(x => x.Capacity)
            .HasColumnName("capacity")
            .IsRequired();

        builder.Property(x => x.ReservedCount)
            .HasColumnName("reserved_count")
            .IsRequired();

        builder.Property(x => x.SlotStatus)
            .HasColumnName("slot_status")
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .IsRequired();

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.FulfillmentMethodOutletId, x.SlotCode })
            .IsUnique()
            .HasDatabaseName("ux_pickup_slots_d08294ab");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_slots_8275d8d5");

        builder.HasOne<E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities.FulfillmentMethodOutlet>()
            .WithMany()
            .HasForeignKey(x => x.FulfillmentMethodOutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_slots_5580f869");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_pickup_slots_window_end", "window_end > window_start");
            t.HasCheckConstraint("ck_pickup_slots_capacity", "capacity >= 0");
            t.HasCheckConstraint("ck_pickup_slots_reserved_count", "reserved_count >= 0 AND reserved_count <= capacity");
            t.HasCheckConstraint("ck_pickup_slots_row_version", "row_version >= 0");
            t.HasCheckConstraint("ck_pickup_slots_slot_status", "slot_status IN ('OPEN', 'FULL', 'CLOSED', 'CANCELLED')");
        });
        // </second-brain-constraints>
    }
}

