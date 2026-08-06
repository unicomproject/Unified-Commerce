using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionPaymentReviewConfiguration : IEntityTypeConfiguration<SubscriptionPaymentReview>
{
    public void Configure(EntityTypeBuilder<SubscriptionPaymentReview> builder)
    {
        builder.ToTable("subscription_payment_reviews");
        builder.HasKey(x => x.Id).HasName("pk_subscription_payment_reviews");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.PaymentId).HasColumnName("payment_id").IsRequired();
        builder.Property(x => x.InvoiceId).HasColumnName("invoice_id").IsRequired();
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(40).IsRequired();
        builder.Property(x => x.StatusBefore).HasColumnName("status_before").HasMaxLength(40).IsRequired();
        builder.Property(x => x.StatusAfter).HasColumnName("status_after").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ActorType).HasColumnName("actor_type").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ActorId).HasColumnName("actor_id");
        builder.Property(x => x.ReviewNote).HasColumnName("review_note").HasColumnType("text");
        builder.Property(x => x.ReasonCode).HasColumnName("reason_code").HasMaxLength(100);
        builder.Property(x => x.IdempotencyKeyHash).HasColumnName("idempotency_key_hash").HasMaxLength(64).IsRequired();
        builder.Property(x => x.RequestHash).HasColumnName("request_hash").HasMaxLength(64).IsRequired();
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(x => x.PaymentVersion).HasColumnName("payment_version").IsRequired();
        builder.Property(x => x.SubmittedAmountSnapshot).HasColumnName("submitted_amount_snapshot").HasPrecision(18, 2);
        builder.Property(x => x.ExpectedAmountSnapshot).HasColumnName("expected_amount_snapshot").HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.CurrencySnapshot).HasColumnName("currency_snapshot").HasMaxLength(3).IsRequired();
        builder.Property(x => x.EvidenceIdSnapshot).HasColumnName("evidence_id_snapshot");
        builder.Property(x => x.EvidenceVersionSnapshot).HasColumnName("evidence_version_snapshot");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.HasOne<SubscriptionPaymentTransaction>().WithMany().HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_subscription_payment_reviews_payment");
        builder.HasOne<SubscriptionInvoice>().WithMany().HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_subscription_payment_reviews_invoice");
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_subscription_payment_reviews_tenant");
        builder.HasOne<E_POS.Domain.Modules.Platform.PlatformAdmin.Entities.PlatformUser>().WithMany().HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_subscription_payment_reviews_actor");
        builder.HasIndex(x => new { x.PaymentId, x.IdempotencyKeyHash }).IsUnique()
            .HasDatabaseName("uq_subscription_payment_reviews_payment_idempotency");
        builder.HasIndex(x => new { x.PaymentId, x.CreatedAt }).HasDatabaseName("ix_subscription_payment_reviews_payment_created");
    }
}
