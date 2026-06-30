using E_POS.Domain.Modules.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Notification.Configurations;

public sealed class NotificationEventConfiguration : IEntityTypeConfiguration<NotificationEvent>
{
    public void Configure(EntityTypeBuilder<NotificationEvent> builder)
    {
        builder.ToTable("notification_events");

        builder.HasKey(x => x.Id).HasName("pk_notification_events");

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

        builder.Property(x => x.EventNumber)
            .HasColumnName("event_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120);

        builder.Property(x => x.NotificationEventTypeId)
            .HasColumnName("notification_event_type_id")
            .IsRequired();

        builder.HasOne<NotificationEventType>()
            .WithMany()
            .HasForeignKey(x => x.NotificationEventTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_events_notification_event_type_id_notification_event_types");

        builder.HasIndex(x => new { x.TenantId, x.EventNumber })
            .IsUnique()
            .HasDatabaseName("uq_notification_events_tenant_id_event_number");

        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("uq_notification_events_tenant_id_idempotency_key")
            .HasFilter("idempotency_key IS NOT NULL");
    }
}

