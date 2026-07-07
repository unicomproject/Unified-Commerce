using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Configurations;

public sealed class TenantSettingConfiguration : IEntityTypeConfiguration<TenantSetting>
{
    public void Configure(EntityTypeBuilder<TenantSetting> builder)
    {
        builder.ToTable("tenant_settings");

        builder.HasKey(x => x.Id).HasName("pk_tenant_settings");

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
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.SettingDefinitionId)
            .HasColumnName("setting_definition_id")
            .IsRequired();
            
        builder.Property(x => x.SettingValue)
            .HasColumnName("setting_value")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.CreatedByPlatformUserId).HasColumnName("created_by_platform_user_id");
        builder.Property(x => x.UpdatedByPlatformUserId).HasColumnName("updated_by_platform_user_id");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_settings_tenant_id_tenants");

        builder.HasOne<SettingDefinition>()
            .WithMany()
            .HasForeignKey(x => x.SettingDefinitionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_settings_setting_definition_id_setting_definitions");

        builder.HasIndex(x => new { x.TenantId, x.SettingDefinitionId })
            .IsUnique()
            .HasDatabaseName("uq_tenant_settings_tenant_id_setting_definition_id");
    }
}




