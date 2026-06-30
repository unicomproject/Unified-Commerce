using E_POS.Domain.Modules.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Notification.Configurations;

public sealed class NotificationDeliveryAttemptConfiguration : IEntityTypeConfiguration<NotificationDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<NotificationDeliveryAttempt> builder)
    {
        builder.ToTable("notification_delivery_attempts");

        builder.HasKey(x => x.Id).HasName("pk_notification_delivery_attempts");

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

        builder.Property(x => x.AttemptNumber)
            .HasColumnName("attempt_number");

        builder.Property(x => x.NotificationChannelId)
            .HasColumnName("notification_channel_id")
            .IsRequired();

        builder.Property(x => x.NotificationMessageId)
            .HasColumnName("notification_message_id")
            .IsRequired();

        builder.HasOne<NotificationMessage>()
            .WithMany()
            .HasForeignKey(x => x.NotificationMessageId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_delivery_attempts_notification_message_id_notification_messages");

        builder.HasOne<NotificationChannel>()
            .WithMany()
            .HasForeignKey(x => x.NotificationChannelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_delivery_attempts_notification_channel_id_notification_channels");

        builder.HasIndex(x => new { x.NotificationMessageId, x.AttemptNumber })
            .IsUnique()
            .HasDatabaseName("uq_notification_delivery_attempts_notification_message_id_attempt_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_notification_delivery_attempts_attempt_number", "attempt_number > 0")); 
    }
}

