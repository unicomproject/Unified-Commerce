using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Discount.Configurations;

public sealed class ExpiryDiscountRuleConfiguration : IEntityTypeConfiguration<ExpiryDiscountRule>
{
    public void Configure(EntityTypeBuilder<ExpiryDiscountRule> builder)
    {
        builder.ToTable("expiry_discount_rules");

        builder.HasKey(x => x.Id).HasName("pk_expiry_discount_rules");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.DiscountPolicyId).HasColumnName("discount_policy_id").IsRequired();
        builder.Property(x => x.RuleCode).HasColumnName("rule_code").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.RuleName).HasColumnName("rule_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text").IsRequired(false);
        builder.Property(x => x.RequireManagerApproval).HasColumnName("requires_manager_approval").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.IsAutoApply).HasColumnName("is_auto_apply").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_expiry_discount_rules_tenant_id_tenants");
        
        builder.HasOne<DiscountPolicy>().WithMany().HasForeignKey(x => new { x.TenantId, x.DiscountPolicyId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_expiry_discount_rules_discount_policy_id_discount_policies");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_expiry_discount_rules_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_expiry_discount_rules_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.RuleCode }).IsUnique().HasDatabaseName("uq_expiry_discount_rules_tenant_id_rule_code");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_expiry_discount_rules_tenant_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_expiry_discount_rules_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}


