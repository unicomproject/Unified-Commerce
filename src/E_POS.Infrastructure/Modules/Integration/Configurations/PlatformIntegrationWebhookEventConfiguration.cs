using E_POS.Domain.Modules.Integration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Integration.Configurations;

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

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.ExternalEventId)
            .HasColumnName("external_event_id");

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120);

        builder.Property(x => x.IntegrationProviderId)
            .HasColumnName("integration_provider_id")
            .IsRequired();

        builder.Property(x => x.PlatformIntegrationId)
            .HasColumnName("platform_integration_id")
            .IsRequired();

        builder.HasOne<IntegrationProvider>()
            .WithMany()
            .HasForeignKey(x => x.IntegrationProviderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integration_webhook_events_integration_provider_id_integration_providers");

        builder.HasOne<PlatformIntegration>()
            .WithMany()
            .HasForeignKey(x => x.PlatformIntegrationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integration_webhook_events_platform_integration_id_platform_integrations");

        builder.HasIndex(x => new { x.IntegrationProviderId, x.ExternalEventId })
            .IsUnique()
            .HasDatabaseName("uq_platform_webhook_events_provider_external_event")
            .HasFilter("external_event_id IS NOT NULL");

        builder.HasIndex(x => new { x.IntegrationProviderId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("uq_platform_webhook_events_provider_idempotency_key")
            .HasFilter("idempotency_key IS NOT NULL");
    }
}

