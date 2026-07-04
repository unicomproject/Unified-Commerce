using E_POS.Domain.Modules.Integration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Integration.Configurations;

public sealed class PlatformIntegrationConfiguration : IEntityTypeConfiguration<PlatformIntegration>
{
    public void Configure(EntityTypeBuilder<PlatformIntegration> builder)
    {
        builder.ToTable("platform_integrations");

        builder.HasKey(x => x.Id).HasName("pk_platform_integrations");

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

        builder.Property(x => x.IntegrationProviderId)
            .HasColumnName("integration_provider_id")
            .IsRequired();

        builder.Property(x => x.IntegrationCode)
            .HasColumnName("integration_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.IntegrationName)
            .HasColumnName("integration_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.IntegrationCategory)
            .HasColumnName("integration_category")
            .IsRequired();

        builder.Property(x => x.IntegrationStatus)
            .HasColumnName("integration_status")
            .IsRequired();

        builder.Property(x => x.Environment)
            .HasColumnName("environment")
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired(false);

        builder.Property(x => x.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.Property(x => x.IsDefault)
            .HasColumnName("is_default")
            .IsRequired();

        builder.Property(x => x.BaseUrl)
            .HasColumnName("base_url")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.InboundWebhookUrl)
            .HasColumnName("inbound_webhook_url")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.ConnectedAt)
            .HasColumnName("connected_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.DisconnectedAt)
            .HasColumnName("disconnected_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.LastSuccessfulRequestAt)
            .HasColumnName("last_successful_request_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.LastFailedRequestAt)
            .HasColumnName("last_failed_request_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.LastFailureReason)
            .HasColumnName("last_failure_reason")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => x.IntegrationCode)
            .IsUnique()
            .HasDatabaseName("ux_platform_integrations_04f0d1d4");

        builder.HasOne<E_POS.Domain.Modules.Integration.Entities.IntegrationProvider>()
            .WithMany()
            .HasForeignKey(x => x.IntegrationProviderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integrations_3bea47ad");

        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Currency>()
            .WithMany()
            .HasForeignKey(x => x.CurrencyCode)
            .HasPrincipalKey(x => x.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integrations_ea0ba30a");

        builder.HasOne<E_POS.Domain.Modules.PlatformAdministration.Entities.PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByPlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integrations_132de113");
        // </second-brain-constraints>
    }
}