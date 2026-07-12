using E_POS.Domain.Modules.Shared.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.Notification.Configurations;

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
            .IsRequired(false);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.NotificationEventId)
            .HasColumnName("notification_event_id")
            .IsRequired();

        builder.Property(x => x.NotificationTemplateVersionId)
            .HasColumnName("notification_template_version_id")
            .IsRequired(false);

        builder.Property(x => x.NotificationChannelId)
            .HasColumnName("notification_channel_id")
            .IsRequired();

        builder.Property(x => x.MessageNumber)
            .HasColumnName("message_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.MessageType)
            .HasColumnName("message_type")
            .IsRequired();

        builder.Property(x => x.ChannelType)
            .HasColumnName("channel_type")
            .IsRequired();

        builder.Property(x => x.RecipientType)
            .HasColumnName("recipient_type")
            .IsRequired();

        builder.Property(x => x.PlatformUserId)
            .HasColumnName("platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.TenantUserId)
            .HasColumnName("tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired(false);

        builder.Property(x => x.RecipientName)
            .HasColumnName("recipient_name")
            .HasColumnType("varchar(180)")
            .HasMaxLength(180)
            .IsRequired(false);

        builder.Property(x => x.RecipientEmail)
            .HasColumnName("recipient_email")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(x => x.RecipientPhone)
            .HasColumnName("recipient_phone")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(x => x.RecipientAddressJson)
            .HasColumnName("recipient_address_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.TitleText)
            .HasColumnName("title_text")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(x => x.BodyTextMapped)
            .HasColumnName("body_text_mapped")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.BodyHtmlMapped)
            .HasColumnName("body_html_mapped")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.ActionUrlMapped)
            .HasColumnName("action_url_mapped")
            .HasColumnType("varchar(700)")
            .HasMaxLength(700)
            .IsRequired(false);

        builder.Property(x => x.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(x => x.MessageStatus)
            .HasColumnName("message_status")
            .IsRequired();

        builder.Property(x => x.ScheduledAt)
            .HasColumnName("scheduled_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.SentAt)
            .HasColumnName("sent_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.DeliveredAt)
            .HasColumnName("delivered_at")
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

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.MessageNumber })
            .IsUnique()
            .HasDatabaseName("ux_notification_messages_02c0854c");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_messages_bf6a679d");

        builder.HasOne<E_POS.Domain.Modules.Shared.Notification.Entities.NotificationEvent>()
            .WithMany()
            .HasForeignKey(x => x.NotificationEventId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_messages_c47ed18f");

        builder.HasOne<E_POS.Domain.Modules.Shared.Notification.Entities.NotificationTemplateVersion>()
            .WithMany()
            .HasForeignKey(x => x.NotificationTemplateVersionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_messages_8e53e11f");

        builder.HasOne<E_POS.Domain.Modules.Shared.Notification.Entities.NotificationChannel>()
            .WithMany()
            .HasForeignKey(x => x.NotificationChannelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_messages_9fc42c44");

        builder.HasOne<E_POS.Domain.Modules.Platform.PlatformAdmin.Entities.PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_messages_46b00891");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.TenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_messages_d2afb07b");

        builder.HasOne<E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_messages_06db3c2c");
        // </second-brain-constraints>
    }
}

