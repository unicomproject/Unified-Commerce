using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.FulfilmentPickup.Configurations;

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

        builder.Ignore(x => x.UpdatedAt);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.FulfillmentOrderId)
            .HasColumnName("fulfillment_order_id")
            .IsRequired();

        builder.Property(x => x.SequenceNumber)
            .HasColumnName("sequence_number")
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .IsRequired();

        builder.Property(x => x.OldStatus)
            .HasColumnName("old_status")
            .IsRequired(false);

        builder.Property(x => x.NewStatus)
            .HasColumnName("new_status")
            .IsRequired(false);

        builder.Property(x => x.EventNote)
            .HasColumnName("event_note")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.EventPayloadJson)
            .HasColumnName("event_payload_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.EventAt)
            .HasColumnName("event_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.EventByTenantUserId)
            .HasColumnName("event_by_tenant_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_order_events_8fff23fa");

        builder.HasOne<E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities.FulfillmentOrder>()
            .WithMany()
            .HasForeignKey(x => x.FulfillmentOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_order_events_beeeae6b");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.EventByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_order_events_ad8a6f3d");
        // </second-brain-constraints>
    }
}

