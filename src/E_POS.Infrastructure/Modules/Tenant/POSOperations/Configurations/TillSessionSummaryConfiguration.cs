using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Configurations;

public sealed class TillSessionSummaryConfiguration : IEntityTypeConfiguration<TillSessionSummary>
{
    public void Configure(EntityTypeBuilder<TillSessionSummary> builder)
    {
        builder.ToTable("till_session_summaries");

        builder.HasKey(x => x.Id).HasName("pk_till_session_summaries");

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

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id")
            .IsRequired();

        builder.Property(x => x.TillId)
            .HasColumnName("till_id")
            .IsRequired();

        builder.Property(x => x.TillSessionId)
            .HasColumnName("till_session_id")
            .IsRequired();

        builder.Property(x => x.CashierTenantUserId)
            .HasColumnName("cashier_tenant_user_id")
            .IsRequired();

        builder.Property(x => x.SessionOpenedAt)
            .HasColumnName("session_opened_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.SessionClosedAt)
            .HasColumnName("session_closed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.OpeningCashAmount)
            .HasColumnName("opening_cash_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.ExpectedCashAmount)
            .HasColumnName("expected_cash_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.CountedCashAmount)
            .HasColumnName("counted_cash_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.CashDifferenceAmount)
            .HasColumnName("cash_difference_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.GrossSalesAmount)
            .HasColumnName("gross_sales_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.TaxAmount)
            .HasColumnName("tax_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.ChargeAmount)
            .HasColumnName("charge_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.NetSalesAmount)
            .HasColumnName("net_sales_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.RefundAmount)
            .HasColumnName("refund_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.VoidAmount)
            .HasColumnName("void_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.OrderCount)
            .HasColumnName("order_count")
            .IsRequired();

        builder.Property(x => x.RefundCount)
            .HasColumnName("refund_count")
            .IsRequired();

        builder.Property(x => x.VoidCount)
            .HasColumnName("void_count")
            .IsRequired();

        builder.Property(x => x.SummaryStatus)
            .HasColumnName("summary_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.GeneratedAt)
            .HasColumnName("generated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ApprovedAt)
            .HasColumnName("approved_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ApprovedByTenantUserId)
            .HasColumnName("approved_by_tenant_user_id");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_summaries_tenant_id_tenants");

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.OutletId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_summaries_outlet_id_outlets");

        builder.HasOne<Till>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TillId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_summaries_till_id_tills");

        builder.HasOne<TillSession>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TillSessionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_summaries_till_session_id_till_sessions");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CashierTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_summaries_cashier_tenant_user_id_tenant_users");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.ApprovedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_summaries_approved_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.TillSessionId })
            .IsUnique()
            .HasDatabaseName("uq_till_session_summaries_till_session_id");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_till_session_summaries_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_till_session_summaries_summary_status", "summary_status IN ('GENERATED', 'APPROVED', 'REJECTED')");
            t.HasCheckConstraint("ck_till_session_summaries_opening_cash_amount", "opening_cash_amount >= 0");
            t.HasCheckConstraint("ck_till_session_summaries_expected_cash_amount", "expected_cash_amount >= 0");
            t.HasCheckConstraint("ck_till_session_summaries_counted_cash_amount", "counted_cash_amount >= 0");
            t.HasCheckConstraint("ck_till_session_summaries_gross_sales_amount", "gross_sales_amount >= 0");
            t.HasCheckConstraint("ck_till_session_summaries_discount_amount", "discount_amount >= 0");
            t.HasCheckConstraint("ck_till_session_summaries_tax_amount", "tax_amount >= 0");
            t.HasCheckConstraint("ck_till_session_summaries_charge_amount", "charge_amount >= 0");
            t.HasCheckConstraint("ck_till_session_summaries_net_sales_amount", "net_sales_amount >= 0");
            t.HasCheckConstraint("ck_till_session_summaries_refund_amount", "refund_amount >= 0");
            t.HasCheckConstraint("ck_till_session_summaries_void_amount", "void_amount >= 0");
            t.HasCheckConstraint("ck_till_session_summaries_order_count", "order_count >= 0");
            t.HasCheckConstraint("ck_till_session_summaries_refund_count", "refund_count >= 0");
            t.HasCheckConstraint("ck_till_session_summaries_void_count", "void_count >= 0");
        });
    }
}



