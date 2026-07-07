using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Configurations;

public sealed class PlatformSettingConfiguration : IEntityTypeConfiguration<PlatformSetting>
{
    public void Configure(EntityTypeBuilder<PlatformSetting> builder)
    {
        builder.ToTable("platform_settings");

        builder.HasKey(x => x.Id).HasName("pk_platform_settings");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.SettingKey)
            .HasColumnName("setting_key")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.SettingValue)
            .HasColumnName("setting_value")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.IsSecret)
            .HasColumnName("is_secret")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByPlatformUserId)
            .HasColumnName("updated_by_platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.SettingKey)
            .IsUnique()
            .HasDatabaseName("uq_platform_settings_setting_key");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByPlatformUserId)
            .HasConstraintName("fk_platform_settings_updated_by_platform_user_id_platform_users")
            .OnDelete(DeleteBehavior.SetNull);
    }
}


