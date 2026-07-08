using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Discount.Configurations;

public sealed class DiscountPolicyConfiguration : IEntityTypeConfiguration<DiscountPolicy>
{
    public void Configure(EntityTypeBuilder<DiscountPolicy> builder)
    {
        builder.ToTable("discount_policies");

        builder.HasKey(x => x.Id).HasName("pk_discount_policies");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.UpdatedBy);
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.DiscountTypeId).HasColumnName("discount_type_id").IsRequired();
        builder.Property(x => x.DiscountPolicyCode).HasColumnName("discount_policy_code").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.DiscountPolicyName).HasColumnName("discount_policy_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text").IsRequired(false);
        builder.Property(x => x.DiscountScope).HasColumnName("discount_scope").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.DiscountValue).HasColumnName("discount_value").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasColumnType("char(3)").HasMaxLength(3).IsRequired(false);
        builder.Property(x => x.MaxDiscountAmount).HasColumnName("max_discount_amount").HasPrecision(18, 4).IsRequired(false);
        builder.Property(x => x.MinOrderAmount).HasColumnName("min_order_amount").HasPrecision(18, 4).IsRequired(false);
        builder.Property(x => x.MinQuantity).HasColumnName("min_quantity").HasPrecision(18, 4).IsRequired(false);
        builder.Property(x => x.RequiresManagerApproval).HasColumnName("requires_manager_approval").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.IsStackable).HasColumnName("is_stackable").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.StackingGroupCode).HasColumnName("stacking_group_code").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired(false);
        builder.Property(x => x.Priority).HasColumnName("priority").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.StartsAt).HasColumnName("starts_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.EndsAt).HasColumnName("ends_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policies_tenant_id_tenants");
        builder.HasOne<DiscountType>().WithMany().HasForeignKey(x => x.DiscountTypeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policies_discount_type_id_discount_types");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).HasPrincipalKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policies_currency_code_currencies");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policies_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policies_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.DiscountPolicyCode }).IsUnique().HasDatabaseName("uq_discount_policies_tenant_id_discount_policy_code");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_discount_policies_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_discount_policies_discount_scope", "discount_scope IN ('ORDER', 'LINE', 'PRODUCT', 'CATEGORY', 'BRAND', 'COLLECTION', 'BATCH')");
            t.HasCheckConstraint("ck_discount_policies_discount_value", "discount_value >= 0");
            t.HasCheckConstraint("ck_discount_policies_amounts", "(max_discount_amount IS NULL OR max_discount_amount >= 0) AND (min_order_amount IS NULL OR min_order_amount >= 0) AND (min_quantity IS NULL OR min_quantity > 0)");
            t.HasCheckConstraint("ck_discount_policies_priority", "priority >= 0");
            t.HasCheckConstraint("ck_discount_policies_period", "ends_at IS NULL OR starts_at IS NULL OR ends_at >= starts_at");
            t.HasCheckConstraint("ck_discount_policies_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        });
    }
}


