using E_POS.Domain.Modules.Integration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Integration.Configurations;

public sealed class PlatformIntegrationRequestLogConfiguration : IEntityTypeConfiguration<PlatformIntegrationRequestLog>
{
    public void Configure(EntityTypeBuilder<PlatformIntegrationRequestLog> builder)
    {
        builder.ToTable("platform_integration_request_logs");

        builder.HasKey(x => x.Id).HasName("pk_platform_integration_request_logs");

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

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DurationMs)
            .HasColumnName("duration_ms");

        builder.Property(x => x.IntegrationProviderId)
            .HasColumnName("integration_provider_id")
            .IsRequired();

        builder.Property(x => x.PlatformIntegrationId)
            .HasColumnName("platform_integration_id")
            .IsRequired();

        builder.Property(x => x.ResponseStatusCode)
            .HasColumnName("response_status_code");

        builder.HasOne<IntegrationProvider>()
            .WithMany()
            .HasForeignKey(x => x.IntegrationProviderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integration_request_logs_integration_provider_id_integration_providers");

        builder.HasOne<PlatformIntegration>()
            .WithMany()
            .HasForeignKey(x => x.PlatformIntegrationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integration_request_logs_platform_integration_id_platform_integrations");

        builder.ToTable(t => t.HasCheckConstraint("ck_platform_integration_request_logs_duration_ms", "duration_ms IS NULL OR duration_ms >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_platform_integration_request_logs_response_status_code", "response_status_code IS NULL OR response_status_code >= 100")); 
    }
}

