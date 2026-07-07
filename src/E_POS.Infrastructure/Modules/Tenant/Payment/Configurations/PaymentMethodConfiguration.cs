using E_POS.Domain.Modules.Tenant.Payment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Payment.Configurations;

public sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("payment_methods");

        builder.HasKey(x => x.Id).HasName("pk_payment_methods");

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

        builder.Property(x => x.MethodCode)
            .HasColumnName("method_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.MethodName)
            .HasColumnName("method_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.MethodType)
            .HasColumnName("method_type")
            .IsRequired();

        builder.Property(x => x.IsActiveForPos)
            .HasColumnName("is_active_for_pos")
            .IsRequired();

        builder.Property(x => x.IsActiveForOnline)
            .HasColumnName("is_active_for_online")
            .IsRequired();

        builder.Property(x => x.RequiresManualConfirmation)
            .HasColumnName("requires_manual_confirmation")
            .IsRequired();

        builder.Property(x => x.SupportsRefund)
            .HasColumnName("supports_refund")
            .IsRequired();

        builder.Property(x => x.RequiresReference)
            .HasColumnName("requires_reference")
            .IsRequired();

        builder.Property(x => x.AllowsChange)
            .HasColumnName("allows_change")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.MethodCode })
            .IsUnique()
            .HasDatabaseName("ux_payment_methods_d1d0cc7d");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_payment_methods_tenant_id_id");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_payment_methods_61d22a01");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_payment_methods_8c525868");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_payment_methods_b8bcb1a7");
        // <second-brain-checks>
        builder.ToTable(t => t.HasCheckConstraint("ck_payment_methods_222cc075", "sort_order >= 0"));
        // </second-brain-checks>

        // </second-brain-constraints>
    }
}

