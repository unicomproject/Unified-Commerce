using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Configurations;

public sealed class HardwareConfigurationChangeAuditConfiguration
    : IEntityTypeConfiguration<HardwareConfigurationChangeAudit>
{
    public void Configure(EntityTypeBuilder<HardwareConfigurationChangeAudit> builder)
    {
        builder.ToTable("hardware_configuration_change_audits");
        builder.HasKey(x => x.Id).HasName("pk_hardware_configuration_change_audits");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.PosDeviceId).HasColumnName("pos_device_id").IsRequired();
        builder.Property(x => x.HardwareDeviceId).HasColumnName("hardware_device_id").IsRequired();
        builder.Property(x => x.TillId).HasColumnName("till_id");
        builder.Property(x => x.TillSessionId).HasColumnName("till_session_id");
        builder.Property(x => x.OldVersion).HasColumnName("old_version").IsRequired();
        builder.Property(x => x.NewVersion).HasColumnName("new_version").IsRequired();
        builder.Property(x => x.ChangeType).HasColumnName("change_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ChangeReason).HasColumnName("change_reason").HasColumnType("varchar(500)").HasMaxLength(500);
        builder.Property(x => x.SafeBeforeJson).HasColumnName("safe_before_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.SafeAfterJson).HasColumnName("safe_after_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ChangedByTenantUserId).HasColumnName("changed_by_tenant_user_id").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => new { x.TenantId, x.OutletId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PosDevice>().WithMany().HasForeignKey(x => new { x.TenantId, x.PosDeviceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<HardwareDevice>().WithMany().HasForeignKey(x => x.HardwareDeviceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Till>().WithMany().HasForeignKey(x => new { x.TenantId, x.TillId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TillSession>().WithMany().HasForeignKey(x => x.TillSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.ChangedByTenantUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.HardwareDeviceId, x.NewVersion })
            .IsUnique()
            .HasDatabaseName("uq_hardware_configuration_audit_version");
    }
}
