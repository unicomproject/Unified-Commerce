using E_POS.Domain.Modules.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Notification.Configurations;

public sealed class NotificationReadReceiptConfiguration : IEntityTypeConfiguration<NotificationReadReceipt>
{
    public void Configure(EntityTypeBuilder<NotificationReadReceipt> builder)
    {
        builder.ToTable("notification_read_receipts");

        builder.HasKey(x => x.Id).HasName("pk_notification_read_receipts");

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

        builder.Property(x => x.NotificationInboxItemId)
            .HasColumnName("notification_inbox_item_id")
            .IsRequired();

        builder.Property(x => x.ReadSource)
            .HasColumnName("read_source")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.HasOne<NotificationInboxItem>()
            .WithMany()
            .HasForeignKey(x => x.NotificationInboxItemId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_read_receipts_notification_inbox_item_id_notification_inbox_items");

        builder.ToTable(t => t.HasCheckConstraint("ck_notification_read_receipts_read_source", "read_source IN ('WEB', 'MOBILE', 'POS', 'ADMIN', 'API')")); 
    }
}

