using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionPaymentTransactionConfiguration : IEntityTypeConfiguration<SubscriptionPaymentTransaction>
{
    public void Configure(EntityTypeBuilder<SubscriptionPaymentTransaction> builder)
    {
        builder.ToTable("subscription_payment_transactions");

        builder.HasKey(x => x.Id).HasName("pk_subscription_payment_transactions");

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

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.ProviderTransactionReference)
            .HasColumnName("provider_transaction_reference")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.SubscriptionInvoiceId)
            .HasColumnName("subscription_invoice_id")
            .IsRequired();

        builder.Property(x => x.SubscriptionPaymentLinkId)
            .HasColumnName("subscription_payment_link_id")
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(x => x.InvoiceId)
            .HasColumnName("invoice_id");

        builder.Property(x => x.PaymentLinkId)
            .HasColumnName("payment_link_id");

        builder.Property(x => x.TransactionType)
            .HasColumnName("transaction_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.ProviderName)
            .HasColumnName("provider_name")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120);

        builder.Property(x => x.ProviderTransactionId)
            .HasColumnName("provider_transaction_id")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.TransactionStatus)
            .HasColumnName("transaction_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3);

        builder.Property(x => x.ProviderFee)
            .HasColumnName("provider_fee")
            .HasPrecision(18, 4);

        builder.Property(x => x.NetAmount)
            .HasColumnName("net_amount")
            .HasPrecision(18, 4);

        builder.Property(x => x.PaidAt)
            .HasColumnName("paid_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.FailedAt)
            .HasColumnName("failed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.FailureReason)
            .HasColumnName("failure_reason")
            .HasColumnType("text");

        builder.Property(x => x.ProviderResponseJson)
            .HasColumnName("provider_response_json")
            .HasColumnType("jsonb");

        builder.HasOne<SubscriptionInvoice>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionInvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_payment_transactions_subscription_invoice_id_subscription_invoices");

        builder.HasOne<SubscriptionPaymentLink>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionPaymentLinkId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_payment_transactions_subscription_payment_link_id_subscription_payment_links");

        builder.HasOne<SubscriptionInvoice>()
            .WithMany()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_payment_transactions_invoice_id_subscription_invoices");

        builder.HasOne<SubscriptionPaymentLink>()
            .WithMany()
            .HasForeignKey(x => x.PaymentLinkId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_payment_transactions_payment_link_id_subscription_payment_links");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_payment_transactions_tenant_id_tenants");

        builder.HasIndex(x => x.ProviderTransactionReference)
            .IsUnique()
            .HasDatabaseName("uq_subscription_payment_transactions_provider_transaction_reference");

        builder.HasIndex(x => x.InvoiceId)
            .HasDatabaseName("ix_subscription_payment_transactions_invoice_id");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_subscription_payment_transactions_tenant_id");

        builder.HasIndex(x => x.PaymentLinkId)
            .HasDatabaseName("ix_subscription_payment_transactions_payment_link_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_subscription_payment_transactions_amount", "amount >= 0");
            t.HasCheckConstraint("ck_subscription_payment_transactions_provider_fee", "provider_fee IS NULL OR provider_fee >= 0");
            t.HasCheckConstraint("ck_subscription_payment_transactions_net_amount", "net_amount IS NULL OR net_amount >= 0");
        });
    }
}
