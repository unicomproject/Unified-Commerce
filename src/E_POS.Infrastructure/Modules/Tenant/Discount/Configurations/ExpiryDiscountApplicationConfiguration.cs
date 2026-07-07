using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Discount.Configurations;

public sealed class ExpiryDiscountApplicationConfiguration : IEntityTypeConfiguration<ExpiryDiscountApplication>
{
    public void Configure(EntityTypeBuilder<ExpiryDiscountApplication> builder)
    {
        builder.ToTable("expiry_discount_applications");

        builder.HasKey(x => x.Id).HasName("pk_expiry_discount_applications");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.UpdatedBy);
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ExpiryDiscountRuleId).HasColumnName("expiry_discount_rule_id").IsRequired();
        builder.Property(x => x.ExpiryDiscountRuleTierId).HasColumnName("expiry_discount_rule_tier_id").IsRequired();
        builder.Property(x => x.ProductBatchId).HasColumnName("product_batch_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.DiscountPercent).HasColumnName("discount_percent").HasPrecision(8, 4).IsRequired();
        builder.Property(x => x.ApplicationSource).HasColumnName("application_source").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ApplicationStatus).HasColumnName("application_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.AppliedFrom).HasColumnName("applied_from").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.AppliedUntil).HasColumnName("applied_until").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.ApprovedByTenantUserId).HasColumnName("approved_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.ApprovedAt).HasColumnName("approved_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.ApprovalNote).HasColumnName("approval_note").HasColumnType("text").IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_expiry_discount_applications_tenant_id_tenants");
        
        builder.HasOne<ExpiryDiscountRule>().WithMany().HasForeignKey(x => new { x.TenantId, x.ExpiryDiscountRuleId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_expiry_discount_applications_expiry_discount_rule_id_expiry_discount_rules");
        builder.HasOne<ExpiryDiscountRuleTier>().WithMany().HasForeignKey(x => new { x.TenantId, x.ExpiryDiscountRuleTierId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_expiry_discount_applications_expiry_discount_rule_tier_id_expiry_discount_rule_tiers");
        builder.HasOne<ProductBatch>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductBatchId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_expiry_discount_applications_product_batch_id_product_batches");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => new { x.TenantId, x.OutletId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_expiry_discount_applications_outlet_id_outlets");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.ApprovedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_expiry_discount_applications_approved_by_tenant_user_id_tenant_users");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_expiry_discount_applications_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_expiry_discount_applications_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.ProductBatchId, x.OutletId }).IsUnique().HasDatabaseName("uq_expiry_discount_applications_active_batch_outlet").HasFilter("application_status = 'ACTIVE'");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_expiry_discount_applications_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_expiry_discount_applications_discount_percent", "discount_percent >= 0 AND discount_percent <= 100");
            t.HasCheckConstraint("ck_expiry_discount_applications_period", "applied_until IS NULL OR applied_from IS NULL OR applied_until >= applied_from");
            t.HasCheckConstraint("ck_expiry_discount_applications_status", "application_status IN ('ACTIVE', 'INACTIVE', 'PENDING_APPROVAL', 'APPROVED', 'REJECTED', 'EXPIRED', 'DELETED')");
        });
    }
}


