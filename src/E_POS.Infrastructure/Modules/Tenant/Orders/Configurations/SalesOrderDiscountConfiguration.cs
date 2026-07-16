using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Orders.Configurations;

public sealed class SalesOrderDiscountConfiguration : IEntityTypeConfiguration<SalesOrderDiscount>
{
    public void Configure(EntityTypeBuilder<SalesOrderDiscount> builder)
    {
        builder.ToTable("sales_order_discounts");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_discounts");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired();

        builder.Property(x => x.SalesOrderLineId)
            .HasColumnName("sales_order_line_id");

        builder.Property(x => x.DiscountPolicyId)
            .HasColumnName("discount_policy_id");

        builder.Property(x => x.DiscountTypeId)
            .HasColumnName("discount_type_id")
            .IsRequired();

        builder.Property(x => x.DiscountTargetScope)
            .HasColumnName("discount_target_scope")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ApplicationSequence)
            .HasColumnName("application_sequence")
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(x => x.DiscountCodeSnapshot)
            .HasColumnName("discount_code_snapshot")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.DiscountNameSnapshot)
            .HasColumnName("discount_name_snapshot")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.CalculationMethodSnapshot)
            .HasColumnName("calculation_method_snapshot")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.DiscountValue)
            .HasColumnName("discount_value")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.ManualDiscountReason)
            .HasColumnName("manual_discount_reason")
            .HasColumnType("text");

        builder.Property(x => x.ApprovalRequiredSnapshot)
            .HasColumnName("approval_required_snapshot")
            .IsRequired(false);

        builder.Property(x => x.ApprovedByTenantUserId)
            .HasColumnName("approved_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.ApprovedAt)
            .HasColumnName("approved_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.AppliedByTenantUserId)
            .HasColumnName("applied_by_tenant_user_id");

        builder.Property(x => x.AppliedAt)
            .HasColumnName("applied_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_discounts_tenant_id_tenants");

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_discounts_sales_order_id_sales_orders");

        builder.HasOne<SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderLineId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_discounts_sales_order_line_id_sales_order_lines");

        builder.HasOne<DiscountPolicy>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.DiscountPolicyId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_discounts_discount_policy_id_discount_policies");

        builder.HasOne<DiscountType>()
            .WithMany()
            .HasForeignKey(x => x.DiscountTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_discounts_discount_type_id_discount_types");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.AppliedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_discounts_applied_by_tenant_user_id_tenant_users");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.ApprovedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_discounts_approved_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_sales_order_discounts_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_sales_order_discounts_discount_target_scope", "discount_target_scope IN ('ORDER', 'LINE')");
            t.HasCheckConstraint("ck_sales_order_discounts_application_sequence", "application_sequence > 0");
            t.HasCheckConstraint("ck_sales_order_discounts_discount_value", "discount_value >= 0");
            t.HasCheckConstraint("ck_sales_order_discounts_discount_amount", "discount_amount >= 0");
        });
    }
}



