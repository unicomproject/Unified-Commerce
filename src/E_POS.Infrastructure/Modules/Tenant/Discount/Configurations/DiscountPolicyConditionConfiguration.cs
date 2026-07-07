using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Discount.Configurations;

public sealed class DiscountPolicyConditionConfiguration : IEntityTypeConfiguration<DiscountPolicyCondition>
{
    public void Configure(EntityTypeBuilder<DiscountPolicyCondition> builder)
    {
        builder.ToTable("discount_policy_conditions");

        builder.HasKey(x => x.Id).HasName("pk_discount_policy_conditions");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.DiscountPolicyId).HasColumnName("discount_policy_id").IsRequired();
        builder.Property(x => x.ConditionGroupNo).HasColumnName("condition_group_no").IsRequired();
        builder.Property(x => x.GroupOperator).HasColumnName("group_operator").HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.ConditionType).HasColumnName("condition_type").HasColumnType("varchar(60)").HasMaxLength(60).IsRequired();
        builder.Property(x => x.ConditionOperator).HasColumnName("condition_operator").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ConditionValueJson).HasColumnName("condition_value_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_conditions_tenant_id_tenants");
        
        builder.HasOne<DiscountPolicy>().WithMany().HasForeignKey(x => new { x.TenantId, x.DiscountPolicyId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_conditions_discount_policy_id_discount_policies");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_conditions_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_conditions_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_discount_policy_conditions_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_discount_policy_conditions_condition_group_no", "condition_group_no > 0");
            t.HasCheckConstraint("ck_discount_policy_conditions_sort_order", "sort_order >= 0");
            t.HasCheckConstraint("ck_discount_policy_conditions_group_operator", "group_operator IN ('AND', 'OR')");
            t.HasCheckConstraint("ck_discount_policy_conditions_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        });
    }
}


