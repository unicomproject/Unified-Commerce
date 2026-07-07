using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Configurations;

public sealed class HardwareDeviceAssignmentConfiguration : IEntityTypeConfiguration<HardwareDeviceAssignment>
{
    public void Configure(EntityTypeBuilder<HardwareDeviceAssignment> builder)
    {
        builder.ToTable("hardware_device_assignments");

        builder.HasKey(x => x.Id).HasName("pk_hardware_device_assignments");

        builder.Property(x => x.Id).HasColumnName("id");
        
        // We map CreatedAt to ERD's assigned_at conceptually, but ERD actually has both assigned_at and created_at.
        // Wait, ERD:
        // | assigned_at | timestamptz |  | NOT NULL | Assignment timestamp. |
        // There is NO created_at/updated_at in ERD! Oh wait, let's look closely at ERD for hardware_device_assignments:
        // ERD table does NOT list created_at or updated_at for hardware_device_assignments.
        // But AuditableEntity has CreatedAt/UpdatedAt. We'll map them explicitly just in case, but ignore CreatedBy/UpdatedBy.
        // Wait, the ERD says: 
        // hardware_device_assignments
        // id
        // tenant_id
        // outlet_id
        // hardware_device_id
        // till_id
        // pos_device_id
        // is_primary
        // assigned_at
        // assigned_by_tenant_user_id
        // released_at
        // released_by_tenant_user_id
        // release_reason
        // No created_at, no updated_at. I will map CreatedAt to assigned_at? No, I added AssignedAt. 
        // I will ignore CreatedAt and UpdatedAt from the base class.
        
        builder.Ignore(x => x.CreatedAt);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.HardwareDeviceId).HasColumnName("hardware_device_id").IsRequired();
        builder.Property(x => x.TillId).HasColumnName("till_id").IsRequired(false);
        builder.Property(x => x.PosDeviceId).HasColumnName("pos_device_id").IsRequired(false);
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.AssignedAt).HasColumnName("assigned_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.AssignedByTenantUserId).HasColumnName("assigned_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.ReleasedAt).HasColumnName("released_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.ReleasedByTenantUserId).HasColumnName("released_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.ReleaseReason).HasColumnName("release_reason").HasColumnType("text").IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_device_assignments_tenant_id_tenants");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => new { x.TenantId, x.OutletId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_device_assignments_outlet_id_outlets");
        
        builder.HasOne<HardwareDevice>().WithMany().HasForeignKey(x => x.HardwareDeviceId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_device_assignments_hardware_device_id_hardware_devices");
        
        builder.HasOne<Till>().WithMany().HasForeignKey(x => new { x.TenantId, x.TillId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_device_assignments_till_id_tills");
        builder.HasOne<PosDevice>().WithMany().HasForeignKey(x => new { x.TenantId, x.PosDeviceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_device_assignments_pos_device_id_pos_devices");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.AssignedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_device_assignments_assigned_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.ReleasedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_device_assignments_released_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => x.HardwareDeviceId).IsUnique().HasDatabaseName("uq_hardware_device_assignments_hardware_device_id_active").HasFilter("released_at IS NULL");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_hardware_device_assignments_target", "num_nonnulls(till_id, pos_device_id) = 1");
        });
    }
}



