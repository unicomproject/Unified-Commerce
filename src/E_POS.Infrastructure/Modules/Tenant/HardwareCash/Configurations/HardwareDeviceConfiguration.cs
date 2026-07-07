using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Configurations;

public sealed class HardwareDeviceConfiguration : IEntityTypeConfiguration<HardwareDevice>
{
    public void Configure(EntityTypeBuilder<HardwareDevice> builder)
    {
        builder.ToTable("hardware_devices");

        builder.HasKey(x => x.Id).HasName("pk_hardware_devices");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.UpdatedBy);
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.HardwareProfileId).HasColumnName("hardware_profile_id").IsRequired(false);
        builder.Property(x => x.HardwareDeviceCode).HasColumnName("hardware_device_code").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.HardwareDeviceName).HasColumnName("hardware_device_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.HardwareDeviceType).HasColumnName("hardware_device_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ConnectionType).HasColumnName("connection_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Manufacturer).HasColumnName("manufacturer").HasColumnType("varchar(120)").HasMaxLength(120).IsRequired(false);
        builder.Property(x => x.Model).HasColumnName("model").HasColumnType("varchar(120)").HasMaxLength(120).IsRequired(false);
        builder.Property(x => x.SerialNumber).HasColumnName("serial_number").HasColumnType("varchar(120)").HasMaxLength(120).IsRequired(false);
        builder.Property(x => x.AssetTag).HasColumnName("asset_tag").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.FirmwareVersion).HasColumnName("firmware_version").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired(false);
        builder.Property(x => x.ConfigJson).HasColumnName("config_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(x => x.LastSeenAt).HasColumnName("last_seen_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_devices_tenant_id_tenants");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => new { x.TenantId, x.OutletId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_devices_outlet_id_outlets");
        builder.HasOne<HardwareProfile>().WithMany().HasForeignKey(x => new { x.TenantId, x.HardwareProfileId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_devices_hardware_profile_id_hardware_profiles");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_devices_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_devices_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.HardwareDeviceCode }).IsUnique().HasDatabaseName("uq_hardware_devices_tenant_id_hardware_device_code");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_hardware_devices_device_type", "hardware_device_type <> ''");
            t.HasCheckConstraint("ck_hardware_devices_connection_type", "connection_type <> ''");
            t.HasCheckConstraint("ck_hardware_devices_status", "status <> ''");
        });
    }
}



