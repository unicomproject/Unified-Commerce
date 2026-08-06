using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Configurations;

public sealed class PlatformTenantOnboardingDraftConfiguration : IEntityTypeConfiguration<PlatformTenantOnboardingDraft>
{
    public void Configure(EntityTypeBuilder<PlatformTenantOnboardingDraft> b)
    {
        b.ToTable("platform_tenant_onboarding_drafts", t =>
        {
            t.HasCheckConstraint("ck_platform_tenant_onboarding_drafts_status", "status IN ('in_progress','finalizing','completed','discarded','expired')");
            t.HasCheckConstraint("ck_platform_tenant_onboarding_drafts_current_step", "current_step BETWEEN 1 AND 7");
            t.HasCheckConstraint("ck_platform_tenant_onboarding_drafts_completed_mask", "completed_steps_mask BETWEEN 0 AND 127");
            t.HasCheckConstraint("ck_platform_tenant_onboarding_drafts_progress", "progress_percent BETWEEN 0 AND 100");
            t.HasCheckConstraint("ck_platform_tenant_onboarding_drafts_schema", "schema_version > 0");
            t.HasCheckConstraint("ck_platform_tenant_onboarding_drafts_version", "version > 0");
        });
        b.HasKey(x => x.Id).HasName("pk_platform_tenant_onboarding_drafts");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.OwnerPlatformUserId).HasColumnName("owner_platform_user_id").IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(24)").HasMaxLength(24).IsRequired();
        b.Property(x => x.CurrentStep).HasColumnName("current_step").IsRequired();
        b.Property(x => x.CompletedStepsMask).HasColumnName("completed_steps_mask").IsRequired();
        b.Property(x => x.ProgressPercent).HasColumnName("progress_percent").IsRequired();
        b.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
        b.Property(x => x.SchemaVersion).HasColumnName("schema_version").IsRequired();
        b.Property(x => x.TenantCodeNormalized).HasColumnName("tenant_code_normalized").HasColumnType("varchar(60)").HasMaxLength(60);
        b.Property(x => x.TenantSlugNormalized).HasColumnName("tenant_slug_normalized").HasColumnType("varchar(100)").HasMaxLength(100);
        b.Property(x => x.RequestedDomainNormalized).HasColumnName("requested_domain_normalized").HasColumnType("varchar(253)").HasMaxLength(253);
        b.Property(x => x.AdminEmailNormalized).HasColumnName("admin_email_normalized").HasColumnType("varchar(320)").HasMaxLength(320);
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();
        b.Property(x => x.FinalizeIdempotencyKeyHash).HasColumnName("finalize_idempotency_key_hash").HasColumnType("char(64)").HasMaxLength(64);
        b.Property(x => x.FinalizeRequestHash).HasColumnName("finalize_request_hash").HasColumnType("char(64)").HasMaxLength(64);
        b.Property(x => x.CreatedTenantId).HasColumnName("created_tenant_id");
        b.Property(x => x.LastErrorCode).HasColumnName("last_error_code").HasColumnType("varchar(100)").HasMaxLength(100);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.DiscardedAt).HasColumnName("discarded_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.FinalizedAt).HasColumnName("finalized_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.CreatedByPlatformUserId).HasColumnName("created_by_platform_user_id").IsRequired();
        b.Property(x => x.UpdatedByPlatformUserId).HasColumnName("updated_by_platform_user_id").IsRequired();
        b.Ignore(x => x.CreatedBy); b.Ignore(x => x.UpdatedBy);
        b.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.OwnerPlatformUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_onboarding_drafts_owner_platform_users");
        b.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.CreatedByPlatformUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_onboarding_drafts_created_by_platform_users");
        b.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.UpdatedByPlatformUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_onboarding_drafts_updated_by_platform_users");
        b.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.CreatedTenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_onboarding_drafts_created_tenant");
        b.HasIndex(x => new { x.OwnerPlatformUserId, x.Status, x.UpdatedAt }).HasDatabaseName("ix_onboarding_drafts_owner_status_updated");
        b.HasIndex(x => x.TenantCodeNormalized).HasDatabaseName("ix_onboarding_drafts_tenant_code");
        b.HasIndex(x => x.TenantSlugNormalized).HasDatabaseName("ix_onboarding_drafts_tenant_slug");
        b.HasIndex(x => x.RequestedDomainNormalized).HasDatabaseName("ix_onboarding_drafts_requested_domain");
    }
}
