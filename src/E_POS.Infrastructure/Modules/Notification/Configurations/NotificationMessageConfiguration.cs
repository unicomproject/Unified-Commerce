using E_POS.Domain.Modules.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Notification.Configurations;

public sealed class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        builder.ToTable("notification_messages");

        builder.HasKey(x => x.Id).HasName("pk_notification_messages");

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

        builder.Property(x => x.PlatformUserId)
            .HasColumnName("platform_user_id");

        builder.Property(x => x.TenantUserId)
            .HasColumnName("tenant_user_id");

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(x => x.RecipientType)
            .HasColumnName("recipient_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.MessageNumber)
            .HasColumnName("message_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.NotificationChannelId)
            .HasColumnName("notification_channel_id")
            .IsRequired();

        builder.Property(x => x.NotificationEventId)
            .HasColumnName("notification_event_id")
            .IsRequired();

        builder.Property(x => x.NotificationTemplateVersionId)
            .HasColumnName("notification_template_version_id")
            .IsRequired();

        builder.HasOne<NotificationEvent>()
            .WithMany()
            .HasForeignKey(x => x.NotificationEventId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_messages_notification_event_id_notification_events");

        builder.HasOne<NotificationTemplateVersion>()
            .WithMany()
            .HasForeignKey(x => x.NotificationTemplateVersionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_messages_notification_template_version_id_notification_template_versions");

        builder.HasOne<NotificationChannel>()
            .WithMany()
            .HasForeignKey(x => x.NotificationChannelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_messages_notification_channel_id_notification_channels");

        builder.HasIndex(x => new { x.TenantId, x.MessageNumber })
            .IsUnique()
            .HasDatabaseName("uq_notification_messages_tenant_id_message_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_notification_messages_recipient_type_platform_user_id_tenant_user_id", "(recipient_type = 'PLATFORM_USER' AND platform_user_id IS NOT NULL) OR (recipient_type = 'TENANT_USER' AND tenant_user_id IS NOT NULL) OR (recipient_type = 'CUSTOMER' AND customer_id IS NOT NULL)")); 
    }
}

