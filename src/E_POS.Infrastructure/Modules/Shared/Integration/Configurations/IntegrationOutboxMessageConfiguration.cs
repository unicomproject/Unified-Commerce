using E_POS.Domain.Modules.Shared.Integration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.Integration.Configurations;

public sealed class IntegrationOutboxMessageConfiguration : IEntityTypeConfiguration<IntegrationOutboxMessage>
{
    public void Configure(EntityTypeBuilder<IntegrationOutboxMessage> b)
    {
        b.ToTable("integration_outbox_messages", t =>
        {
            t.HasCheckConstraint("ck_integration_outbox_status", "status IN ('PENDING','PROCESSING','DELIVERED','FAILED_RETRYABLE','FAILED_FINAL')");
            t.HasCheckConstraint("ck_integration_outbox_attempts", "attempt_count >= 0");
            t.HasCheckConstraint("ck_integration_outbox_schema", "payload_schema_version > 0");
            t.HasCheckConstraint("ck_integration_outbox_sequence", "aggregate_sequence > 0");
        });
        b.HasKey(x => x.Id).HasName("pk_integration_outbox_messages");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.MessageType).HasColumnName("message_type").HasColumnType("varchar(120)").HasMaxLength(120).IsRequired();
        b.Property(x => x.AggregateType).HasColumnName("aggregate_type").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        b.Property(x => x.AggregateId).HasColumnName("aggregate_id").IsRequired();
        b.Property(x => x.AggregateSequence).HasColumnName("aggregate_sequence").IsRequired();
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.CorrelationId).HasColumnName("correlation_id").IsRequired();
        b.Property(x => x.CausationId).HasColumnName("causation_id");
        b.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
        b.Property(x => x.PayloadSchemaVersion).HasColumnName("payload_schema_version").IsRequired();
        b.Property(x => x.DeduplicationKey).HasColumnName("deduplication_key").HasColumnType("varchar(180)").HasMaxLength(180).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(24)").HasMaxLength(24).IsRequired();
        b.Property(x => x.AttemptCount).HasColumnName("attempt_count").IsRequired();
        b.Property(x => x.AvailableAt).HasColumnName("available_at").HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.LeaseOwner).HasColumnName("lease_owner").HasColumnType("varchar(120)").HasMaxLength(120);
        b.Property(x => x.LeaseExpiresAt).HasColumnName("lease_expires_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.ProcessedAt).HasColumnName("processed_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.LastErrorCode).HasColumnName("last_error_code").HasColumnType("varchar(100)").HasMaxLength(100);
        b.Property(x => x.SanitizedLastError).HasColumnName("sanitized_last_error").HasColumnType("varchar(500)").HasMaxLength(500);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        b.Ignore(x => x.CreatedBy); b.Ignore(x => x.UpdatedBy);
        b.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_integration_outbox_tenant");
        b.HasIndex(x => x.DeduplicationKey).IsUnique().HasDatabaseName("uq_integration_outbox_deduplication_key");
        b.HasIndex(x => new { x.AggregateType, x.AggregateId, x.AggregateSequence }).IsUnique().HasDatabaseName("uq_integration_outbox_aggregate_sequence");
        b.HasIndex(x => new { x.Status, x.AvailableAt }).HasDatabaseName("ix_integration_outbox_claim");
    }
}
