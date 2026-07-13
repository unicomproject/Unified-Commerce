using E_POS.Domain.Modules.Customer.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Discount.Configurations;

public sealed class PosDiscountApplicationConfiguration : IEntityTypeConfiguration<PosDiscountApplication>
{
    public void Configure(EntityTypeBuilder<PosDiscountApplication> builder)
    {
        builder.ToTable("pos_discount_applications", t =>
        {
            t.HasCheckConstraint("ck_pos_discount_applications_source", "discount_source IN ('POLICY', 'MANUAL')");
            t.HasCheckConstraint("ck_pos_discount_applications_scope", "discount_scope IN ('ORDER', 'LINE')");
            t.HasCheckConstraint("ck_pos_discount_applications_status", "application_status IN ('PENDING_APPROVAL', 'APPROVED', 'REJECTED', 'EXPIRED', 'APPLIED', 'CANCELLED')");
            t.HasCheckConstraint("ck_pos_discount_applications_values", "requested_value > 0 AND cashier_limit_snapshot >= 0 AND absolute_limit_snapshot > 0");
            t.HasCheckConstraint("ck_pos_discount_applications_amounts", "cart_subtotal_snapshot >= 0 AND eligible_subtotal_snapshot >= 0 AND discount_amount_snapshot >= 0 AND total_after_discount_snapshot >= 0");
        });
        builder.HasKey(x => x.Id).HasName("pk_pos_discount_applications");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.DiscountPolicyId).HasColumnName("discount_policy_id").IsRequired();
        builder.Property(x => x.DiscountTypeId).HasColumnName("discount_type_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.TillId).HasColumnName("till_id").IsRequired();
        builder.Property(x => x.TillSessionId).HasColumnName("till_session_id").IsRequired();
        builder.Property(x => x.PosDeviceId).HasColumnName("pos_device_id").IsRequired();
        builder.Property(x => x.RequestedByTenantUserId).HasColumnName("requested_by_tenant_user_id").IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.TargetProductVariantId).HasColumnName("target_product_variant_id");
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(x => x.DiscountSource).HasColumnName("discount_source").HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.DiscountScope).HasColumnName("discount_scope").HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.PolicyCodeSnapshot).HasColumnName("policy_code_snapshot").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.PolicyNameSnapshot).HasColumnName("policy_name_snapshot").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.CalculationMethodSnapshot).HasColumnName("calculation_method_snapshot").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.RequestedValue).HasColumnName("requested_value").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CashierLimitSnapshot).HasColumnName("cashier_limit_snapshot").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.AbsoluteLimitSnapshot).HasColumnName("absolute_limit_snapshot").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CartSubtotalSnapshot).HasColumnName("cart_subtotal_snapshot").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.EligibleSubtotalSnapshot).HasColumnName("eligible_subtotal_snapshot").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.DiscountAmountSnapshot).HasColumnName("discount_amount_snapshot").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.TotalAfterDiscountSnapshot).HasColumnName("total_after_discount_snapshot").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasColumnType("char(3)").HasMaxLength(3).IsRequired();
        builder.Property(x => x.CartSnapshotJson).HasColumnName("cart_snapshot_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CartHash).HasColumnName("cart_hash").HasColumnType("char(64)").HasMaxLength(64).IsRequired();
        builder.Property(x => x.RequestReason).HasColumnName("request_reason").HasColumnType("text");
        builder.Property(x => x.ApplicationStatus).HasColumnName("application_status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        builder.Property(x => x.RequiresManagerApproval).HasColumnName("requires_manager_approval").IsRequired();
        builder.Property(x => x.RequestedAt).HasColumnName("requested_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.DecidedByTenantUserId).HasColumnName("decided_by_tenant_user_id");
        builder.Property(x => x.DecidedAt).HasColumnName("decided_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.DecisionNote).HasColumnName("decision_note").HasColumnType("text");
        builder.Property(x => x.SalesOrderId).HasColumnName("sales_order_id");
        builder.Property(x => x.AppliedAt).HasColumnName("applied_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsConcurrencyToken();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.HasIndex(x => new { x.TenantId, x.RequestedByTenantUserId, x.IdempotencyKey }).IsUnique().HasDatabaseName("uq_pos_discount_applications_idempotency");
        builder.HasIndex(x => new { x.TenantId, x.ApplicationStatus, x.ExpiresAt }).HasDatabaseName("ix_pos_discount_applications_status_expiry");
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DiscountPolicy>().WithMany().HasForeignKey(x => new { x.TenantId, x.DiscountPolicyId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DiscountType>().WithMany().HasForeignKey(x => x.DiscountTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => new { x.TenantId, x.OutletId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Till>().WithMany().HasForeignKey(x => new { x.TenantId, x.TillId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TillSession>().WithMany().HasForeignKey(x => new { x.TenantId, x.TillSessionId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PosDevice>().WithMany().HasForeignKey(x => new { x.TenantId, x.PosDeviceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.RequestedByTenantUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.DecidedByTenantUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<E_POS.Domain.Modules.Customer.Entities.Customer>().WithMany().HasForeignKey(x => new { x.TenantId, x.CustomerId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SalesOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesOrderId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.ProductVariant>().WithMany().HasForeignKey(x => new { x.TenantId, x.TargetProductVariantId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).HasPrincipalKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
    }
}
