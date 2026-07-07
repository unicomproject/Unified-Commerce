using E_POS.Domain.Modules.Shared.Integration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.Integration.Configurations;

public sealed class PlatformIntegrationWebhookEventConfiguration : IEntityTypeConfiguration<PlatformIntegrationWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PlatformIntegrationWebhookEvent> builder)
    {
        builder.ToTable("platform_integration_webhook_events");

        builder.HasKey(x => x.Id).HasName("pk_platform_integration_webhook_events");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(x => x.UpdatedAt);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.IntegrationProviderId)
            .HasColumnName("integration_provider_id")
            .IsRequired();

        builder.Property(x => x.PlatformIntegrationId)
            .HasColumnName("platform_integration_id")
            .IsRequired(false);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired(false);

        builder.Property(x => x.ExternalEventId)
            .HasColumnName("external_event_id")
            .HasColumnType("varchar(180)")
            .HasMaxLength(180)
            .IsRequired(false);

        builder.Property(x => x.EventName)
            .HasColumnName("event_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.EventCategory)
            .HasColumnName("event_category")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(x => x.SignatureValid)
            .HasColumnName("signature_valid")
            .IsRequired(false);

        builder.Property(x => x.EventStatus)
            .HasColumnName("event_status")
            .IsRequired();

        builder.Property(x => x.EventPayloadJson)
            .HasColumnName("event_payload_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.HeadersJson)
            .HasColumnName("headers_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.SourceIp)
            .HasColumnName("source_ip")
            .HasColumnType("inet")
            .IsRequired(false);

        builder.Property(x => x.ReceivedSignatureHash)
            .HasColumnName("received_signature_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(x => x.ReceivedAt)
            .HasColumnName("received_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ProcessingStartedAt)
            .HasColumnName("processing_started_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ProcessingError)
            .HasColumnName("processing_error")
            .HasColumnType("text")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.IntegrationProviderId, x.ExternalEventId })
            .IsUnique()
            .HasDatabaseName("ux_platform_integration_webhook_events_be2c7f88");

        builder.HasIndex(x => new { x.IntegrationProviderId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_platform_integration_webhook_events_900a6647");

        builder.HasOne<E_POS.Domain.Modules.Shared.Integration.Entities.IntegrationProvider>()
            .WithMany()
            .HasForeignKey(x => x.IntegrationProviderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integration_webhook_events_dec9d3ba");

        builder.HasOne<E_POS.Domain.Modules.Shared.Integration.Entities.PlatformIntegration>()
            .WithMany()
            .HasForeignKey(x => x.PlatformIntegrationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integration_webhook_events_b9ac16f8");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integration_webhook_events_fe8d195a");
        // </second-brain-constraints>
    }
}

