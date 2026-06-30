using E_POS.Domain.Modules.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.FulfilmentPickup.Configurations;

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

        builder.Property(x => x.Capacity)
            .HasColumnName("capacity");

        builder.Property(x => x.FulfillmentMethodOutletId)
            .HasColumnName("fulfillment_method_outlet_id")
            .IsRequired();

        builder.Property(x => x.ReservedCount)
            .HasColumnName("reserved_count");

        builder.Property(x => x.SlotDate)
            .HasColumnName("slot_date")
            .HasColumnType("date");

        builder.Property(x => x.WindowEnd)
            .HasColumnName("window_end")
            .HasColumnType("time without time zone");

        builder.Property(x => x.WindowStart)
            .HasColumnName("window_start")
            .HasColumnType("time without time zone");

        builder.HasOne<FulfillmentMethodOutlet>()
            .WithMany()
            .HasForeignKey(x => x.FulfillmentMethodOutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_slots_fulfillment_method_outlet_id_fulfillment_method_outlets");

        builder.HasIndex(x => new { x.FulfillmentMethodOutletId, x.SlotDate, x.WindowStart, x.WindowEnd })
            .IsUnique()
            .HasDatabaseName("uq_pickup_slots_fulfillment_method_outlet_id_slot_date_window_start_window_end");

        builder.ToTable(t => t.HasCheckConstraint("ck_pickup_slots_capacity", "capacity >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_pickup_slots_reserved_count", "reserved_count >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_pickup_slots_reserved_count_capacity", "reserved_count <= capacity")); 
    }
}

