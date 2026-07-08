using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.FulfilmentPickup.Configurations;

public sealed class FulfillmentOrderConfiguration : IEntityTypeConfiguration<FulfillmentOrder>
{
    public void Configure(EntityTypeBuilder<FulfillmentOrder> builder)
    {
        builder.ToTable("fulfillment_orders");

        builder.HasKey(x => x.Id).HasName("pk_fulfillment_orders");

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

        builder.Property(x => x.DocumentNumberSequenceId)
            .HasColumnName("document_number_sequence_id")
            .IsRequired(false);

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired();

        builder.Property(x => x.FulfillmentNumber)
            .HasColumnName("fulfillment_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.FulfillmentMethodOutletId)
            .HasColumnName("fulfillment_method_outlet_id")
            .IsRequired();

        builder.Property(x => x.SourceInventoryLocationId)
            .HasColumnName("source_inventory_location_id")
            .IsRequired(false);

        builder.Property(x => x.FulfillmentStatus)
            .HasColumnName("fulfillment_status")
            .IsRequired();

        builder.Property(x => x.RequestedFulfillmentDate)
            .HasColumnName("requested_fulfillment_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(x => x.ScheduledAt)
            .HasColumnName("scheduled_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.PickedAt)
            .HasColumnName("picked_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.PackedAt)
            .HasColumnName("packed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ReadyAt)
            .HasColumnName("ready_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.FulfilledAt)
            .HasColumnName("fulfilled_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.CancelledAt)
            .HasColumnName("cancelled_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.AssignedToTenantUserId)
            .HasColumnName("assigned_to_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.FulfillmentNote)
            .HasColumnName("fulfillment_note")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.FulfillmentNumber })
            .IsUnique()
            .HasDatabaseName("ux_fulfillment_orders_e767fb12");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_orders_93e151fe");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.DocumentNumberSequence>()
            .WithMany()
            .HasForeignKey(x => x.DocumentNumberSequenceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_orders_076928b6");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_orders_dc59b3c6");

        builder.HasOne<E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities.FulfillmentMethodOutlet>()
            .WithMany()
            .HasForeignKey(x => x.FulfillmentMethodOutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_orders_baf8addd");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Inventory.Entities.InventoryLocation>()
            .WithMany()
            .HasForeignKey(x => x.SourceInventoryLocationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_orders_b268c630");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.AssignedToTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_orders_0197fb4f");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_orders_d02b8cf5");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_orders_8d547aff");
        // </second-brain-constraints>
    }
}

