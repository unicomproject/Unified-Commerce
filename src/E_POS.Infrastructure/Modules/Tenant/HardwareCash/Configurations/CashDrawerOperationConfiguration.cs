using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Configurations;

public sealed class CashDrawerOperationConfiguration : IEntityTypeConfiguration<CashDrawerOperation>
{
    public void Configure(EntityTypeBuilder<CashDrawerOperation> builder)
    {
        builder.ToTable("cash_drawer_operations");

        builder.HasKey(x => x.Id).HasName("pk_cash_drawer_operations");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.HardwareDeviceId).HasColumnName("hardware_device_id").IsRequired(false);
        builder.Property(x => x.PosDeviceId).HasColumnName("pos_device_id").IsRequired();
        builder.Property(x => x.TillId).HasColumnName("till_id").IsRequired();
        builder.Property(x => x.TillSessionId).HasColumnName("till_session_id").IsRequired();
        builder.Property(x => x.ProcessedByUserId).HasColumnName("processed_by_user_id").IsRequired();
        builder.Property(x => x.ApproverId).HasColumnName("approver_id").IsRequired(false);
        builder.Property(x => x.RequestId).HasColumnName("request_id").IsRequired();
        builder.Property(x => x.DrawerPurpose).HasColumnName("drawer_purpose").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired(false);
        builder.Property(x => x.BusinessReferenceType).HasColumnName("business_reference_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired(false);
        builder.Property(x => x.BusinessReferenceId).HasColumnName("business_reference_id").IsRequired(false);
        builder.Property(x => x.ConfigurationId).HasColumnName("configuration_id").IsRequired(false);
        builder.Property(x => x.ConfigurationVersion).HasColumnName("configuration_version").IsRequired();
        builder.Property(x => x.DrawerPort).HasColumnName("drawer_port").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.PulseOnTime).HasColumnName("pulse_on_time").IsRequired();
        builder.Property(x => x.PulseOffTime).HasColumnName("pulse_off_time").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ResultCategory).HasColumnName("result_category").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired(false);
        builder.Property(x => x.FailureCategory).HasColumnName("failure_category").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired(false);
        builder.Property(x => x.AgentAccepted).HasColumnName("agent_accepted").IsRequired();
        builder.Property(x => x.PhysicalConfirmation).HasColumnName("physical_confirmation").IsRequired(false);
        builder.Property(x => x.InitiatedAt).HasColumnName("initiated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.PayloadHash).HasColumnName("payload_hash").HasColumnType("varchar(64)").HasMaxLength(64).IsRequired();

        // Foreign keys
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_drawer_operations_tenant_id_tenants");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => new { x.TenantId, x.OutletId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_drawer_operations_outlet_id_outlets");
        builder.HasOne<HardwareDevice>().WithMany().HasForeignKey(x => x.HardwareDeviceId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_drawer_operations_hardware_device_id_hardware_devices");
        builder.HasOne<PosDevice>().WithMany().HasForeignKey(x => new { x.TenantId, x.PosDeviceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_drawer_operations_pos_device_id_pos_devices");
        builder.HasOne<Till>().WithMany().HasForeignKey(x => new { x.TenantId, x.TillId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_drawer_operations_till_id_tills");
        builder.HasOne<TillSession>().WithMany().HasForeignKey(x => x.TillSessionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_drawer_operations_till_session_id_till_sessions");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.ProcessedByUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_drawer_operations_processed_by_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.ApproverId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_drawer_operations_approver_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.RequestId })
            .IsUnique()
            .HasDatabaseName("uq_cash_drawer_operations_tenant_id_request_id");
        builder.HasIndex(x => new { x.TenantId, x.PosDeviceId, x.InitiatedAt })
            .HasDatabaseName("ix_cash_drawer_operations_device_history");
    }
}
