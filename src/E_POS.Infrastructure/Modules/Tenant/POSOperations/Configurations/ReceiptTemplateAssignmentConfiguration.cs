using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Configurations;

public sealed class ReceiptTemplateAssignmentConfiguration : IEntityTypeConfiguration<ReceiptTemplateAssignment>
{
    public void Configure(EntityTypeBuilder<ReceiptTemplateAssignment> builder)
    {
        builder.ToTable("receipt_template_assignments");

        builder.HasKey(x => x.Id).HasName("pk_receipt_template_assignments");

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

        builder.Property(x => x.ReceiptTemplateVersionId)
            .HasColumnName("receipt_template_version_id")
            .IsRequired();

        builder.Property(x => x.AssignmentScope)
            .HasColumnName("assignment_scope")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id");

        builder.Property(x => x.TillId)
            .HasColumnName("till_id");

        builder.Property(x => x.PosDeviceId)
            .HasColumnName("pos_device_id");

        builder.Property(x => x.IsDefault)
            .HasColumnName("is_default")
            .IsRequired();

        builder.Property(x => x.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.EffectiveTo)
            .HasColumnName("effective_to")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_template_assignments_tenant_id_tenants");

        builder.HasOne<ReceiptTemplateVersion>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReceiptTemplateVersionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_template_assignments_receipt_template_version_id_receipt_template_versions");

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.OutletId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_template_assignments_outlet_id_outlets");

        builder.HasOne<Till>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TillId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_template_assignments_till_id_tills");

        builder.HasOne<PosDevice>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.PosDeviceId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_template_assignments_pos_device_id_pos_devices");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_receipt_template_assignments_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_receipt_template_assignments_assignment_scope", "assignment_scope IN ('OUTLET', 'TILL', 'POS_DEVICE')");
            t.HasCheckConstraint("ck_receipt_template_assignments_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
            t.HasCheckConstraint("ck_receipt_template_assignments_effective_dates", "effective_to IS NULL OR effective_to >= effective_from");
            t.HasCheckConstraint("ck_receipt_template_assignments_scope_rules", "(assignment_scope = 'OUTLET' AND outlet_id IS NOT NULL AND till_id IS NULL AND pos_device_id IS NULL) OR (assignment_scope = 'TILL' AND outlet_id IS NULL AND till_id IS NOT NULL AND pos_device_id IS NULL) OR (assignment_scope = 'POS_DEVICE' AND outlet_id IS NULL AND till_id IS NULL AND pos_device_id IS NOT NULL)");
        });
    }
}



