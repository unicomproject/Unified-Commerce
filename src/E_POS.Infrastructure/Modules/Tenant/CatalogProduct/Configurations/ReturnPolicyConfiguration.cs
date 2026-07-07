using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ReturnPolicyConfiguration : IEntityTypeConfiguration<ReturnPolicy>
{
    public void Configure(EntityTypeBuilder<ReturnPolicy> builder)
    {
        builder.ToTable("return_policies");

        builder.HasKey(x => x.Id).HasName("pk_return_policies");

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

        builder.Property(x => x.ReturnPolicyCode)
            .HasColumnName("return_policy_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.ReturnPolicyName)
            .HasColumnName("return_policy_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.ReturnWindowDays)
            .HasColumnName("return_window_days")
            .IsRequired();

        builder.Property(x => x.ExchangeWindowDays)
            .HasColumnName("exchange_window_days")
            .IsRequired();

        builder.Property(x => x.RequiresReceipt)
            .HasColumnName("requires_receipt")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.AllowDefectiveReturn)
            .HasColumnName("allow_defective_return")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.RequiresManagerApproval)
            .HasColumnName("requires_manager_approval")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsDefaultPolicy)
            .HasColumnName("is_default_policy")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_policies_tenant_id_tenants");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_policies_created_by_tenant_user_id_tenant_users");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_policies_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.ReturnPolicyCode })
            .IsUnique()
            .HasDatabaseName("uq_return_policies_tenant_id_return_policy_code");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_return_policies_tenant_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_return_policies_return_window_days", "return_window_days >= 0")); 
        builder.ToTable(t => t.HasCheckConstraint("ck_return_policies_exchange_window_days", "exchange_window_days >= 0")); 
        builder.ToTable(t => t.HasCheckConstraint("ck_return_policies_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')")); 
    }
}


