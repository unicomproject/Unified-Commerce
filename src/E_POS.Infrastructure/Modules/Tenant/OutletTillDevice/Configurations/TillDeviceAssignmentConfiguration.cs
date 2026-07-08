using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Configurations;

public sealed class TillDeviceAssignmentConfiguration : IEntityTypeConfiguration<TillDeviceAssignment>
{
    public void Configure(EntityTypeBuilder<TillDeviceAssignment> builder)
    {
        builder.ToTable("till_device_assignments");
        builder.HasKey(x => x.Id).HasName("pk_till_device_assignments");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.TillId).HasColumnName("till_id").IsRequired();
        builder.Property(x => x.PosDeviceId).HasColumnName("pos_device_id").IsRequired();
        builder.Property(x => x.AssignedAt).HasColumnName("assigned_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.AssignedByTenantUserId).HasColumnName("assigned_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.ReleasedAt).HasColumnName("released_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.ReleasedByTenantUserId).HasColumnName("released_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.ReleaseReason).HasColumnName("release_reason").HasColumnType("text").IsRequired(false);
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_till_device_assignments_tenant_id_tenants");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_till_device_assignments_outlet_id_outlets");
        builder.HasOne<Till>().WithMany().HasForeignKey(x => x.TillId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_till_device_assignments_till_id_tills");
        builder.HasOne<PosDevice>().WithMany().HasForeignKey(x => x.PosDeviceId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_till_device_assignments_pos_device_id_pos_devices");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.AssignedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_till_device_assignments_assigned_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.ReleasedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_till_device_assignments_released_by_tenant_user_id_tenant_users");
        builder.HasIndex(x => x.PosDeviceId).IsUnique().HasDatabaseName("uq_till_device_assignments_active_pos_device").HasFilter("released_at IS NULL");
        builder.HasIndex(x => x.TillId).IsUnique().HasDatabaseName("uq_till_device_assignments_active_till").HasFilter("released_at IS NULL");
    }
}
