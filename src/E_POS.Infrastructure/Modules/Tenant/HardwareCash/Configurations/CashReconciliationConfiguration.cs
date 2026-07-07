using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities; // for Currency and Tenant
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Configurations;

public sealed class CashReconciliationConfiguration : IEntityTypeConfiguration<CashReconciliation>
{
    public void Configure(EntityTypeBuilder<CashReconciliation> builder)
    {
        builder.ToTable("cash_reconciliations");

        builder.HasKey(x => x.Id).HasName("pk_cash_reconciliations");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.TillSessionId).HasColumnName("till_session_id").IsRequired();
        builder.Property(x => x.ReconciliationNumber).HasColumnName("reconciliation_number").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.ExpectedCashAmount).HasColumnName("expected_cash_amount").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CountedCashAmount).HasColumnName("counted_cash_amount").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.DifferenceAmount).HasColumnName("difference_amount").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasColumnType("char(3)").HasMaxLength(3).IsRequired();
        builder.Property(x => x.ReconciliationStatus).HasColumnName("reconciliation_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.DifferenceReason).HasColumnName("difference_reason").HasColumnType("text").IsRequired(false);
        builder.Property(x => x.CalculationDetailsJson).HasColumnName("calculation_details_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(x => x.SubmittedByTenantUserId).HasColumnName("submitted_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.SubmittedAt).HasColumnName("submitted_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.ApprovedByTenantUserId).HasColumnName("approved_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.ApprovedAt).HasColumnName("approved_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.ApprovalNote).HasColumnName("approval_note").HasColumnType("text").IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_reconciliations_tenant_id_tenants");
        builder.HasOne<TillSession>().WithMany().HasForeignKey(x => x.TillSessionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_reconciliations_till_session_id_till_sessions");
        
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).HasPrincipalKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_reconciliations_currency_code_currencies");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.SubmittedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_reconciliations_submitted_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.ApprovedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_reconciliations_approved_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.ReconciliationNumber }).IsUnique().HasDatabaseName("uq_cash_reconciliations_tenant_id_reconciliation_number");
        
        // Removed unique index on TillSessionId as a session could theoretically have multiple reconciliation attempts if previous was rejected, but usually it's one. The ERD does not explicitly specify it is unique, although it often is. I will keep it unique if it was unique before. Ah, previous configuration had:
        // builder.HasIndex(x => x.TillSessionId).IsUnique().HasDatabaseName("uq_cash_reconciliations_till_session_id");
        builder.HasIndex(x => x.TillSessionId).IsUnique().HasDatabaseName("uq_cash_reconciliations_till_session_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_cash_reconciliations_expected_cash_amount", "expected_cash_amount >= 0");
            t.HasCheckConstraint("ck_cash_reconciliations_counted_cash_amount", "counted_cash_amount >= 0");
            t.HasCheckConstraint("ck_cash_reconciliations_status", "reconciliation_status <> ''");
        });
    }
}



