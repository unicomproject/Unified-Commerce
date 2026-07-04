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

        builder.Ignore(x => x.UpdatedAt);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.NotificationEventTypeId)
            .HasColumnName("notification_event_type_id")
            .IsRequired();

        builder.Property(x => x.EventNumber)
            .HasColumnName("event_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.EventCode)
            .HasColumnName("event_code")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.SourceModule)
            .HasColumnName("source_module")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(x => x.SourceReferenceType)
            .HasColumnName("source_reference_type")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(x => x.SourceReferenceId)
            .HasColumnName("source_reference_id")
            .IsRequired(false);

        builder.Property(x => x.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(x => x.EventStatus)
            .HasColumnName("event_status")
            .IsRequired();

        builder.Property(x => x.ScheduledAt)
            .HasColumnName("scheduled_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ProcessingStartedAt)
            .HasColumnName("processing_started_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.FailedAt)
            .HasColumnName("failed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.FailureReason)
            .HasColumnName("failure_reason")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.CancelledAt)
            .HasColumnName("cancelled_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.EventNumber })
            .IsUnique()
            .HasDatabaseName("ux_notification_events_05be9a99");

        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_events_a96480c4");

        builder.HasOne<E_POS.Domain.Modules.Notification.Entities.NotificationEventType>()
            .WithMany()
            .HasForeignKey(x => x.NotificationEventTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_events_96dab6c4");

        builder.HasOne<E_POS.Domain.Modules.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_events_3b90157b");

        builder.HasOne<E_POS.Domain.Modules.PlatformAdministration.Entities.PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByPlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_events_58c4d7af");
        // </second-brain-constraints>
    }
}