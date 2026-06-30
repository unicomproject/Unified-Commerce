using E_POS.Domain.Modules.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Notification.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(x => x.Id).HasName("pk_notification_preferences");

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

        builder.Property(x => x.ChannelType)
            .HasColumnName("channel_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.RecipientType)
            .HasColumnName("recipient_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.NotificationEventTypeId)
            .HasColumnName("notification_event_type_id")
            .IsRequired();

        builder.HasOne<NotificationEventType>()
            .WithMany()
            .HasForeignKey(x => x.NotificationEventTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_preferences_notification_event_type_id_notification_event_types");

        builder.HasIndex(x => new { x.TenantId, x.RecipientType, x.PlatformUserId, x.TenantUserId, x.CustomerId, x.NotificationEventTypeId, x.ChannelType })
            .IsUnique()
            .HasDatabaseName("uq_notification_preferences_tenant_id_recipient_type_platform_user_id_tenant_user_id_customer_id_notification_event_type_id_channel_type");

        builder.ToTable(t => t.HasCheckConstraint("ck_notification_preferences_recipient_type_platform_user_id_tenant_user_id", "(recipient_type = 'PLATFORM_USER' AND platform_user_id IS NOT NULL AND tenant_user_id IS NULL AND customer_id IS NULL) OR (recipient_type = 'TENANT_USER' AND tenant_user_id IS NOT NULL AND platform_user_id IS NULL AND customer_id IS NULL) OR (recipient_type = 'CUSTOMER' AND customer_id IS NOT NULL AND platform_user_id IS NULL AND tenant_user_id IS NULL)")); 
    }
}

