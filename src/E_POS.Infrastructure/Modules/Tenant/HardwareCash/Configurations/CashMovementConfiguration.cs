using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities; // for Tax
using E_POS.Domain.Modules.Tenant.Orders.Entities; // for Order
using E_POS.Domain.Modules.Tenant.Payment.Entities; // for Payment
using E_POS.Domain.Modules.Shared.Refund.Entities; // for Refund
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Configurations;

public sealed class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("cash_movements");

        builder.HasKey(x => x.Id).HasName("pk_cash_movements");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.TillId).HasColumnName("till_id").IsRequired();
        builder.Property(x => x.TillSessionId).HasColumnName("till_session_id").IsRequired();
        builder.Property(x => x.PosDeviceId).HasColumnName("pos_device_id").IsRequired(false);
        builder.Property(x => x.MovementTypeId).HasColumnName("movement_type_id").IsRequired();
        builder.Property(x => x.MovementNumber).HasColumnName("movement_number").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasColumnType("char(3)").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason").HasColumnType("text").IsRequired(false);
        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired(false);
        builder.Property(x => x.PaymentId).HasColumnName("payment_id").IsRequired(false);
        builder.Property(x => x.RefundId).HasColumnName("refund_id").IsRequired(false);
        builder.Property(x => x.PerformedByTenantUserId).HasColumnName("performed_by_tenant_user_id").IsRequired();
        builder.Property(x => x.PerformedAt).HasColumnName("performed_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_movements_tenant_id_tenants");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => new { x.TenantId, x.OutletId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_movements_outlet_id_outlets");
        builder.HasOne<Till>().WithMany().HasForeignKey(x => new { x.TenantId, x.TillId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_movements_till_id_tills");
        builder.HasOne<TillSession>().WithMany().HasForeignKey(x => x.TillSessionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_movements_till_session_id_till_sessions");
        builder.HasOne<PosDevice>().WithMany().HasForeignKey(x => new { x.TenantId, x.PosDeviceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_movements_pos_device_id_pos_devices");
        
        builder.HasOne<CashMovementType>().WithMany().HasForeignKey(x => x.MovementTypeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_movements_movement_type_id_cash_movement_types");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.PerformedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_movements_performed_by_tenant_user_id_tenant_users");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).HasPrincipalKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_movements_currency_code_currencies");

        builder.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_movements_order_id_sales_orders");
        builder.HasOne<SalesPayment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_movements_payment_id_sales_payments");
        builder.HasOne<SalesRefund>().WithMany().HasForeignKey(x => x.RefundId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_movements_refund_id_refunds");

        builder.HasIndex(x => new { x.TenantId, x.MovementNumber }).IsUnique().HasDatabaseName("uq_cash_movements_tenant_id_movement_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_cash_movements_amount", "amount >= 0"));
    }
}



