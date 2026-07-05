using E_POS.Domain.Modules.Payment.Entities;
using E_POS.Domain.Modules.POSOperations.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.POSOperations.Configurations;

public sealed class TillSessionPaymentSummaryConfiguration : IEntityTypeConfiguration<TillSessionPaymentSummary>
{
    public void Configure(EntityTypeBuilder<TillSessionPaymentSummary> builder)
    {
        builder.ToTable("till_session_payment_summaries");

        builder.HasKey(x => x.Id).HasName("pk_till_session_payment_summaries");

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

        builder.Property(x => x.TillSessionSummaryId)
            .HasColumnName("till_session_summary_id")
            .IsRequired();

        builder.Property(x => x.PaymentMethodId)
            .HasColumnName("payment_method_id")
            .IsRequired();

        builder.Property(x => x.SalesAmount)
            .HasColumnName("sales_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.RefundAmount)
            .HasColumnName("refund_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.NetAmount)
            .HasColumnName("net_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.TransactionCount)
            .HasColumnName("transaction_count")
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_payment_summaries_tenant_id_tenants");

        builder.HasOne<TillSessionSummary>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TillSessionSummaryId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_payment_summaries_till_session_summary_id_till_session_summaries");

        builder.HasOne<PaymentMethod>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.PaymentMethodId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_payment_summaries_payment_method_id_payment_methods");

        builder.HasIndex(x => new { x.TenantId, x.TillSessionSummaryId, x.PaymentMethodId })
            .IsUnique()
            .HasDatabaseName("uq_till_session_payment_summaries_till_session_summary_id_payment_method_id");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_till_session_payment_summaries_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_till_session_payment_summaries_sales_amount", "sales_amount >= 0");
            t.HasCheckConstraint("ck_till_session_payment_summaries_refund_amount", "refund_amount >= 0");
            t.HasCheckConstraint("ck_till_session_payment_summaries_transaction_count", "transaction_count >= 0");
        });
    }
}
