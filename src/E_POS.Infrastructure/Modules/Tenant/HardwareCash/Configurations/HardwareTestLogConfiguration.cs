using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Configurations;

public sealed class HardwareTestLogConfiguration : IEntityTypeConfiguration<HardwareTestLog>
{
    public void Configure(EntityTypeBuilder<HardwareTestLog> builder)
    {
        builder.ToTable("hardware_test_logs");

        builder.HasKey(x => x.Id).HasName("pk_hardware_test_logs");

        builder.Property(x => x.Id).HasColumnName("id");
        
        // This is an append-only log table. ERD has created_at but no updated_at.
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.HardwareDeviceId).HasColumnName("hardware_device_id").IsRequired();
        builder.Property(x => x.InitiatedFromPosDeviceId).HasColumnName("initiated_from_pos_device_id").IsRequired(false);
        builder.Property(x => x.TestedByTenantUserId).HasColumnName("tested_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.TestType).HasColumnName("test_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.TestStatus).HasColumnName("test_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ResultMessage).HasColumnName("result_message").HasColumnType("text").IsRequired(false);
        builder.Property(x => x.ResultPayloadJson).HasColumnName("result_payload_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(x => x.TestedAt).HasColumnName("tested_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_test_logs_tenant_id_tenants");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => new { x.TenantId, x.OutletId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_test_logs_outlet_id_outlets");
        builder.HasOne<HardwareDevice>().WithMany().HasForeignKey(x => x.HardwareDeviceId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_test_logs_hardware_device_id_hardware_devices");
        builder.HasOne<PosDevice>().WithMany().HasForeignKey(x => new { x.TenantId, x.InitiatedFromPosDeviceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_test_logs_initiated_from_pos_device_id_pos_devices");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.TestedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_test_logs_tested_by_tenant_user_id_tenant_users");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_hardware_test_logs_test_type", "test_type <> ''");
            t.HasCheckConstraint("ck_hardware_test_logs_test_status", "test_status <> ''");
        });
    }
}



