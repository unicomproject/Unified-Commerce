using E_POS.Domain.Modules.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.FulfilmentPickup.Configurations;

public sealed class FulfillmentOrderEventConfiguration : IEntityTypeConfiguration<FulfillmentOrderEvent>
{
    public void Configure(EntityTypeBuilder<FulfillmentOrderEvent> builder)
    {
        builder.ToTable("fulfillment_order_events");

        builder.HasKey(x => x.Id).HasName("pk_fulfillment_order_events");

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

        builder.Property(x => x.FulfillmentOrderId)
            .HasColumnName("fulfillment_order_id")
            .IsRequired();

        builder.Property(x => x.SequenceNumber)
            .HasColumnName("sequence_number");

        builder.HasOne<FulfillmentOrder>()
            .WithMany()
            .HasForeignKey(x => x.FulfillmentOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_order_events_fulfillment_order_id_fulfillment_orders");

        builder.HasIndex(x => new { x.FulfillmentOrderId, x.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("uq_fulfillment_order_events_fulfillment_order_id_sequence_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_fulfillment_order_events_sequence_number", "sequence_number > 0")); 
    }
}

