using E_POS.Domain.Modules.Shared.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.Notification.Configurations;

public sealed class NotificationReadReceiptConfiguration : IEntityTypeConfiguration<NotificationReadReceipt>
{
    public void Configure(EntityTypeBuilder<NotificationReadReceipt> builder)
    {
        builder.ToTable("notification_read_receipts");

        builder.HasKey(x => x.Id).HasName("pk_notification_read_receipts");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Ignore(x => x.CreatedAt);

        builder.Ignore(x => x.UpdatedAt);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.NotificationInboxItemId)
            .HasColumnName("notification_inbox_item_id")
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

        builder.Property(x => x.ReadAt)
            .HasColumnName("read_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

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
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_read_receipts_cdc7161e");

        builder.HasOne<E_POS.Domain.Modules.Shared.Notification.Entities.NotificationInboxItem>()
            .WithMany()
            .HasForeignKey(x => x.NotificationInboxItemId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_read_receipts_faddad41");

        builder.HasOne<E_POS.Domain.Modules.Shared.Notification.Entities.NotificationMessage>()
            .WithMany()
            .HasForeignKey(x => x.NotificationMessageId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_read_receipts_b4b32f67");

        builder.HasOne<E_POS.Domain.Modules.Platform.PlatformAdmin.Entities.PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_read_receipts_aeb35063");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.TenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_read_receipts_77d5c9dd");

        builder.HasOne<E_POS.Domain.Modules.Customer.Entities.Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_read_receipts_a2927305");
        // </second-brain-constraints>
    }
}

