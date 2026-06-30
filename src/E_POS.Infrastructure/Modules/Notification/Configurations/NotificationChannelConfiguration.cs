using E_POS.Domain.Modules.Integration.Entities;
using E_POS.Domain.Modules.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Notification.Configurations;

public sealed class NotificationChannelConfiguration : IEntityTypeConfiguration<NotificationChannel>
{
    public void Configure(EntityTypeBuilder<NotificationChannel> builder)
    {
        builder.ToTable("notification_channels");

        builder.HasKey(x => x.Id).HasName("pk_notification_channels");

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

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ChannelType)
            .HasColumnName("channel_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.ChannelCode)
            .HasColumnName("channel_code")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.PlatformIntegrationId)
            .HasColumnName("platform_integration_id")
            .IsRequired();

        builder.HasOne<PlatformIntegration>()
            .WithMany()
            .HasForeignKey(x => x.PlatformIntegrationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_channels_platform_integration_id_platform_integrations");

        builder.HasIndex(x => new { x.TenantId, x.ChannelCode })
            .IsUnique()
            .HasDatabaseName("uq_notification_channels_tenant_id_channel_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_notification_channels_channel_type", "channel_type IN ('EMAIL', 'SMS', 'WHATSAPP', 'PUSH', 'IN_APP')")); 
    }
}

