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

        builder.Ignore(x => x.CreatedAt);

        builder.Ignore(x => x.UpdatedAt);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.NotificationMessageId)
            .HasColumnName("notification_message_id")
            .IsRequired();

        builder.Property(x => x.AttemptNumber)
            .HasColumnName("attempt_number")
            .IsRequired();

        builder.Property(x => x.ChannelType)
            .HasColumnName("channel_type")
            .IsRequired();

        builder.Property(x => x.ProviderName)
            .HasColumnName("provider_name")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(x => x.ProviderMessageId)
            .HasColumnName("provider_message_id")
            .HasColumnType("varchar(160)")
            .HasMaxLength(160)
            .IsRequired(false);

        builder.Property(x => x.RequestPayloadJson)
            .HasColumnName("request_payload_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.ResponsePayloadJson)
            .HasColumnName("response_payload_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.ResponseStatusCode)
            .HasColumnName("response_status_code")
            .IsRequired(false);

        builder.Property(x => x.ErrorCode)
            .HasColumnName("error_code")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.AttemptedAt)
            .HasColumnName("attempted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.NotificationMessageId, x.AttemptNumber })
            .IsUnique()
            .HasDatabaseName("ux_notification_delivery_attempts_59643ebe");

        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_delivery_attempts_117f17e1");

        builder.HasOne<E_POS.Domain.Modules.Notification.Entities.NotificationMessage>()
            .WithMany()
            .HasForeignKey(x => x.NotificationMessageId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_delivery_attempts_f1272a17");
        // </second-brain-constraints>
    }
}