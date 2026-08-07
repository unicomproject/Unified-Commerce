using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Configurations;

public sealed class PosOrderHoldEventConfiguration : IEntityTypeConfiguration<PosOrderHoldEvent>
{
    public void Configure(EntityTypeBuilder<PosOrderHoldEvent> builder)
    {
        builder.ToTable("pos_order_hold_events");

        builder.HasKey(x => x.Id).HasName("pk_pos_order_hold_events");

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

        builder.Property(x => x.HoldId)
            .HasColumnName("hold_id")
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.EventAt)
            .HasColumnName("event_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.EventByTenantUserId)
            .HasColumnName("event_by_tenant_user_id");

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id");

        builder.Property(x => x.TillId)
            .HasColumnName("till_id");

        builder.Property(x => x.TillSessionId)
            .HasColumnName("till_session_id");

        builder.Property(x => x.PosDeviceId)
            .HasColumnName("pos_device_id");

        builder.Property(x => x.HoldNumber)
            .HasColumnName("hold_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id");

        builder.Property(x => x.PreviousStatus)
            .HasColumnName("previous_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.NewStatus)
            .HasColumnName("new_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.CorrelationId)
            .HasColumnName("correlation_id")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pos_order_hold_events_tenant_id_tenants");

        builder.HasOne<PosOrderHold>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.HoldId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pos_order_hold_events_hold_id_pos_order_holds");

        builder.HasIndex(x => new { x.TenantId, x.HoldId });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_pos_order_hold_events_event_type",
                "event_type IN ('PARK_CREATED', 'PARK_IDEMPOTENT_REPLAY', 'PARK_RECALLED', 'PARK_CANCELLED', 'PARK_EXPIRED')");
        });
    }
}
