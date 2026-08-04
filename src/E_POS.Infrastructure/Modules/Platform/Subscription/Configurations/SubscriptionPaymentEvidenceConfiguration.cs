using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionPaymentEvidenceConfiguration : IEntityTypeConfiguration<SubscriptionPaymentEvidence>
{
    public void Configure(EntityTypeBuilder<SubscriptionPaymentEvidence> builder)
    {
        builder.ToTable("subscription_payment_evidence");
        builder.HasKey(x => x.Id).HasName("pk_subscription_payment_evidence");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.PaymentId).HasColumnName("payment_id").IsRequired();
        builder.Property(x => x.InvoiceId).HasColumnName("invoice_id").IsRequired();
        builder.Property(x => x.BlobContainer).HasColumnName("blob_container").HasMaxLength(100).IsRequired();
        builder.Property(x => x.StorageKey).HasColumnName("storage_key").HasMaxLength(700).IsRequired();
        builder.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.SafeFileName).HasColumnName("safe_file_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.FileSize).HasColumnName("file_size").IsRequired();
        builder.Property(x => x.Sha256).HasColumnName("sha256").HasMaxLength(64).IsRequired();
        builder.Property(x => x.EvidenceType).HasColumnName("evidence_type").HasMaxLength(40).IsRequired();
        builder.Property(x => x.UploadedByType).HasColumnName("uploaded_by_type").HasMaxLength(40).IsRequired();
        builder.Property(x => x.UploadedById).HasColumnName("uploaded_by_id");
        builder.Property(x => x.SubmissionVersion).HasColumnName("submission_version").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(x => x.SupersededAt).HasColumnName("superseded_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ScanStatus).HasColumnName("scan_status").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ScanFailureCode).HasColumnName("scan_failure_code").HasMaxLength(100);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.HasOne<SubscriptionPaymentTransaction>().WithMany().HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_subscription_payment_evidence_payment");
        builder.HasOne<SubscriptionInvoice>().WithMany().HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_subscription_payment_evidence_invoice");
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_subscription_payment_evidence_tenant");
        builder.HasIndex(x => new { x.PaymentId, x.SubmissionVersion }).HasDatabaseName("ix_subscription_payment_evidence_payment_submission");
        builder.HasIndex(x => x.StorageKey).IsUnique().HasDatabaseName("uq_subscription_payment_evidence_storage_key");
        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_payment_evidence_file_size", "file_size > 0"));
    }
}
