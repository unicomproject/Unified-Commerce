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
            .IsRequired(false);

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

        builder.Property(x => x.ProviderEventId).HasColumnName("provider_event_id").HasMaxLength(180);
        builder.Property(x => x.ProviderCheckoutUrl).HasColumnName("provider_checkout_url").HasMaxLength(700);
        builder.Property(x => x.ProviderCustomerReferenceId).HasColumnName("provider_customer_reference_id").HasMaxLength(180);
        builder.Property(x => x.ProviderStatus).HasColumnName("provider_status").HasMaxLength(80);
        builder.Property(x => x.ProviderCallbackReceiptJson).HasColumnName("provider_callback_receipt_json").HasColumnType("jsonb");

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

        builder.Property(x => x.TenantSubscriptionId).HasColumnName("tenant_subscription_id");
        builder.Property(x => x.ExpectedAmount).HasColumnName("expected_amount").HasPrecision(18, 2);
        builder.Property(x => x.SubmittedAmount).HasColumnName("submitted_amount").HasPrecision(18, 2);
        builder.Property(x => x.ApprovedAmount).HasColumnName("approved_amount").HasPrecision(18, 2);
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(40);
        builder.Property(x => x.ManualReference).HasColumnName("manual_reference").HasMaxLength(255);
        builder.Property(x => x.ManualReferenceNormalized).HasColumnName("manual_reference_normalized").HasMaxLength(255);
        builder.Property(x => x.PaymentDate).HasColumnName("payment_date").HasColumnType("timestamp with time zone");
        builder.Property(x => x.SubmittedAt).HasColumnName("submitted_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.SubmittedByType).HasColumnName("submitted_by_type").HasMaxLength(40);
        builder.Property(x => x.SubmittedById).HasColumnName("submitted_by_id");
        builder.Property(x => x.PayerNote).HasColumnName("payer_note").HasColumnType("text");
        builder.Property(x => x.VerifiedAt).HasColumnName("verified_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.VerifiedByPlatformUserId).HasColumnName("verified_by_platform_user_id");
        builder.Property(x => x.ReviewNote).HasColumnName("review_note").HasColumnType("text");
        builder.Property(x => x.RejectionReasonCode).HasColumnName("rejection_reason_code").HasMaxLength(100);
        builder.Property(x => x.FailureCode).HasColumnName("failure_code").HasMaxLength(100);
        builder.Property(x => x.LastCommandIdempotencyKeyHash).HasColumnName("last_command_idempotency_key_hash").HasMaxLength(64);
        builder.Property(x => x.LastCommandRequestHash).HasColumnName("last_command_request_hash").HasMaxLength(64);
        builder.Property(x => x.SubmissionVersion).HasColumnName("submission_version").HasDefaultValue(0);
        builder.Property(x => x.Version).HasColumnName("version").HasDefaultValue(1).IsConcurrencyToken();

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

        builder.HasOne<TenantSubscription>()
            .WithMany()
            .HasForeignKey(x => x.TenantSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_payment_transactions_tenant_subscription_id");

        builder.HasIndex(x => x.ProviderTransactionReference)
            .IsUnique()
            .HasDatabaseName("uq_subscription_payment_transactions_provider_transaction_reference");

        builder.HasIndex(x => x.InvoiceId)
            .HasDatabaseName("ix_subscription_payment_transactions_invoice_id");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_subscription_payment_transactions_tenant_id");

        builder.HasIndex(x => x.TenantSubscriptionId)
            .HasDatabaseName("ix_subscription_payment_transactions_tenant_subscription_id");

        builder.HasIndex(x => x.PaymentLinkId)
            .HasDatabaseName("ix_subscription_payment_transactions_payment_link_id");

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL")
            .HasDatabaseName("uq_subscription_payment_transactions_idempotency_key");

        builder.HasIndex(x => new { x.ProviderName, x.ProviderEventId })
            .IsUnique().HasFilter("provider_event_id IS NOT NULL")
            .HasDatabaseName("uq_subscription_payment_transactions_provider_event");

        builder.HasIndex(x => new { x.TenantId, x.InvoiceId, x.ManualReferenceNormalized })
            .HasFilter("manual_reference_normalized IS NOT NULL")
            .HasDatabaseName("ix_subscription_payment_transactions_manual_reference");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_subscription_payment_transactions_amount", "amount >= 0");
            t.HasCheckConstraint("ck_subscription_payment_transactions_provider_fee", "provider_fee IS NULL OR provider_fee >= 0");
            t.HasCheckConstraint("ck_subscription_payment_transactions_net_amount", "net_amount IS NULL OR net_amount >= 0");
            t.HasCheckConstraint("ck_subscription_payment_transactions_expected_amount", "expected_amount >= 0");
            t.HasCheckConstraint("ck_subscription_payment_transactions_submitted_amount", "submitted_amount IS NULL OR submitted_amount > 0");
        });
    }
}
