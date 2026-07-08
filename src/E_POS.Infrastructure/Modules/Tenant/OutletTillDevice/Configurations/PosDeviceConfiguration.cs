using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Configurations;

public sealed class PosDeviceConfiguration : IEntityTypeConfiguration<PosDevice>
{
    public void Configure(EntityTypeBuilder<PosDevice> builder)
    {
        builder.ToTable("pos_devices");
        builder.HasKey(x => x.Id).HasName("pk_pos_devices");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.DeviceCode).HasColumnName("device_code").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.DeviceName).HasColumnName("device_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DeviceType).HasColumnName("device_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Platform).HasColumnName("platform").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired(false);
        builder.Property(x => x.AppVersion).HasColumnName("app_version").HasColumnType("varchar(50)").HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.DeviceFingerprintHash).HasColumnName("device_fingerprint_hash").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired(false);
        builder.Property(x => x.IsTrusted).HasColumnName("is_trusted").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.PairedAt).HasColumnName("paired_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.PairedByTenantUserId).HasColumnName("paired_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UnpairedAt).HasColumnName("unpaired_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.UnpairedByTenantUserId).HasColumnName("unpaired_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.LastSeenAt).HasColumnName("last_seen_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id").IsRequired(false);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_pos_devices_tenant_id_tenants");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_pos_devices_outlet_id_outlets");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_pos_devices_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_pos_devices_updated_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.PairedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_pos_devices_paired_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UnpairedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_pos_devices_unpaired_by_tenant_user_id_tenant_users");
        builder.HasIndex(x => new { x.TenantId, x.DeviceCode }).IsUnique().HasDatabaseName("uq_pos_devices_tenant_id_device_code");
        builder.ToTable(t => t.HasCheckConstraint("ck_pos_devices_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}
