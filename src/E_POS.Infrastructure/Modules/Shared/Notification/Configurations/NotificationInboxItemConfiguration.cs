using E_POS.Domain.Modules.Shared.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.Notification.Configurations;

public sealed class NotificationInboxItemConfiguration : IEntityTypeConfiguration<NotificationInboxItem>
{
    public void Configure(EntityTypeBuilder<NotificationInboxItem> builder)
    {
        builder.ToTable("notification_inbox_items");

        builder.HasKey(x => x.Id).HasName("pk_notification_inbox_items");

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

        builder.Property(x => x.NotificationMessageId)
            .HasColumnName("notification_message_id")
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

        builder.Property(x => x.TitleText)
            .HasColumnName("title_text")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(x => x.BodyText)
            .HasColumnName("body_text")
            .HasColumnType("varchar(700)")
            .HasMaxLength(700)
            .IsRequired(false);

        builder.Property(x => x.LinkUrl)
            .HasColumnName("link_url")
            .HasColumnType("varchar(700)")
            .HasMaxLength(700)
            .IsRequired(false);

        builder.Property(x => x.InboxStatus)
            .HasColumnName("inbox_status")
            .IsRequired();

        builder.Property(x => x.DeliveredAt)
            .HasColumnName("delivered_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ArchivedAt)
            .HasColumnName("archived_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ReadAt)
            .HasColumnName("read_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("varchar(45)")
            .HasMaxLength(45)
            .IsRequired(false);

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasColumnType("text")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => x.NotificationMessageId)
            .IsUnique()
            .HasDatabaseName("ux_notification_inbox_items_5e98743f");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_inbox_items_4798709c");

        builder.HasOne<E_POS.Domain.Modules.Shared.Notification.Entities.NotificationMessage>()
            .WithMany()
            .HasForeignKey(x => x.NotificationMessageId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_inbox_items_34305b9a");

        builder.HasOne<E_POS.Domain.Modules.Platform.PlatformAdmin.Entities.PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_inbox_items_36b291a8");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.TenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_inbox_items_f47d7f24");

        builder.HasOne<E_POS.Domain.Modules.Customer.Entities.Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_inbox_items_bfd2c259");
        // </second-brain-constraints>
    }
}

