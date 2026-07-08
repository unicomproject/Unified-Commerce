using E_POS.Domain.Modules.Shared.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.Notification.Configurations;

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
            .IsRequired(false);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired(false);

        builder.Property(x => x.ChannelCode)
            .HasColumnName("channel_code")
            .HasColumnType("varchar(90)")
            .HasMaxLength(90)
            .IsRequired();

        builder.Property(x => x.ChannelName)
            .HasColumnName("channel_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.ChannelType)
            .HasColumnName("channel_type")
            .IsRequired();

        builder.Property(x => x.IsSystemChannel)
            .HasColumnName("is_system_channel")
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.Property(x => x.ProviderName)
            .HasColumnName("provider_name")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(x => x.ProviderConfigJson)
            .HasColumnName("provider_config_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByPlatformUserId)
            .HasColumnName("updated_by_platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.ArchivedAt)
            .HasColumnName("archived_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ArchivedByPlatformUserId)
            .HasColumnName("archived_by_platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.ArchivedByTenantUserId)
            .HasColumnName("archived_by_tenant_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.ChannelCode })
            .IsUnique()
            .HasDatabaseName("ux_notification_channels_5708f727");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_channels_bd6812f0");

        builder.HasOne<E_POS.Domain.Modules.Platform.PlatformAdmin.Entities.PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByPlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_channels_068b9801");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_channels_e57275e8");

        builder.HasOne<E_POS.Domain.Modules.Platform.PlatformAdmin.Entities.PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByPlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_channels_6582c928");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_channels_0fe25ca8");

        builder.HasOne<E_POS.Domain.Modules.Platform.PlatformAdmin.Entities.PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.ArchivedByPlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_channels_6c57bfdf");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.ArchivedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_channels_c29ac65e");
        // </second-brain-constraints>
    }
}

