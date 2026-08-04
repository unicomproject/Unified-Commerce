using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Configurations;

public sealed class PlatformTenantOnboardingOperationConfiguration : IEntityTypeConfiguration<PlatformTenantOnboardingOperation>
{
    public void Configure(EntityTypeBuilder<PlatformTenantOnboardingOperation> b)
    {
        b.ToTable("platform_tenant_onboarding_operations", t =>
        {
            t.HasCheckConstraint("ck_onboarding_operations_status", "status IN ('PROCESSING','SUCCEEDED','FAILED_RETRYABLE','FAILED_FINAL')");
            t.HasCheckConstraint("ck_onboarding_operations_attempts", "attempt_count >= 0");
            t.HasCheckConstraint("ck_onboarding_operations_provisioning_status", "provisioning_status IN ('PROCESSING','SUCCEEDED','FAILED_RETRYABLE','FAILED_FINAL')");
            t.HasCheckConstraint("ck_onboarding_operations_payment_status",
                "payment_status IN ('NOT_REQUIRED','PENDING','CONFIRMED','FAILED','WAIVED','AWAITING_PAYMENT','PAYMENT_SUBMITTED','UNDER_REVIEW','ACTION_REQUIRED','PAID','REJECTED','EXPIRED','CANCELLED','DEFERRED')");
            t.HasCheckConstraint("ck_onboarding_operations_invitation_status",
                "invitation_status IN ('NOT_ELIGIBLE','PENDING_ACTIVATION','PENDING','SENT','FAILED','ACCEPTED','EXPIRED')");
        });
        b.HasKey(x => x.Id).HasName("pk_platform_tenant_onboarding_operations");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.DraftId).HasColumnName("draft_id").IsRequired();
        b.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(x => x.OperationType).HasColumnName("operation_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(32)").HasMaxLength(32).IsRequired();
        b.Property(x => x.ProvisioningStatus).HasColumnName("provisioning_status").HasColumnType("varchar(32)").HasMaxLength(32).IsRequired();
        b.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasColumnType("varchar(24)").HasMaxLength(24).IsRequired();
        b.Property(x => x.InvitationStatus).HasColumnName("invitation_status").HasColumnType("varchar(24)").HasMaxLength(24).IsRequired();
        b.Property(x => x.IdempotencyKeyHash).HasColumnName("idempotency_key_hash").HasColumnType("char(64)").HasMaxLength(64).IsRequired();
        b.Property(x => x.RequestHash).HasColumnName("request_hash").HasColumnType("char(64)").HasMaxLength(64).IsRequired();
        b.Property(x => x.AttemptCount).HasColumnName("attempt_count").IsRequired();
        b.Property(x => x.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.NextRetryAt).HasColumnName("next_retry_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.FailureCode).HasColumnName("failure_code").HasColumnType("varchar(100)").HasMaxLength(100);
        b.Property(x => x.SanitizedFailureDetails).HasColumnName("sanitized_failure_details").HasColumnType("varchar(500)").HasMaxLength(500);
        b.Property(x => x.ResultReference).HasColumnName("result_reference").HasColumnType("varchar(160)").HasMaxLength(160);
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        b.Ignore(x => x.CreatedBy); b.Ignore(x => x.UpdatedBy);
        b.HasOne<PlatformTenantOnboardingDraft>().WithMany().HasForeignKey(x => x.DraftId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_onboarding_operations_draft");
        b.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_onboarding_operations_tenant");
        b.HasIndex(x => x.DraftId).IsUnique().HasDatabaseName("uq_onboarding_operations_draft");
        b.HasIndex(x => x.TenantId).IsUnique().HasDatabaseName("uq_onboarding_operations_tenant");
        b.HasIndex(x => new { x.Status, x.NextRetryAt }).HasDatabaseName("ix_onboarding_operations_retry");
    }
}
