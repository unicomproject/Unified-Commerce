using E_POS.Domain.Modules.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Notification.Configurations;

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

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.InboxStatus)
            .HasColumnName("inbox_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.NotificationMessageId)
            .HasColumnName("notification_message_id")
            .IsRequired();

        builder.HasOne<NotificationMessage>()
            .WithMany()
            .HasForeignKey(x => x.NotificationMessageId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_inbox_items_notification_message_id_notification_messages");

        builder.HasIndex(x => x.NotificationMessageId)
            .IsUnique()
            .HasDatabaseName("uq_notification_inbox_items_notification_message_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_notification_inbox_items_inbox_status", "inbox_status IN ('UNREAD', 'READ', 'ARCHIVED', 'DELETED')")); 
    }
}

