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

        builder.Property(x => x.RequestDirection)
            .HasColumnName("request_direction")
            .IsRequired();

        builder.Property(x => x.RequestType)
            .HasColumnName("request_type")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.HttpMethod)
            .HasColumnName("http_method")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.RequestUrl)
            .HasColumnName("request_url")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.RequestHeadersJson)
            .HasColumnName("request_headers_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.RequestBodyHash)
            .HasColumnName("request_body_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(x => x.ResponseStatusCode)
            .HasColumnName("response_status_code")
            .IsRequired(false);

        builder.Property(x => x.ResponseHeadersJson)
            .HasColumnName("response_headers_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.ResponseBodyHash)
            .HasColumnName("response_body_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(x => x.RequestStatus)
            .HasColumnName("request_status")
            .IsRequired();

        builder.Property(x => x.ErrorCode)
            .HasColumnName("error_code")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(x => x.CorrelationId)
            .HasColumnName("correlation_id")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(x => x.RequestedAt)
            .HasColumnName("requested_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.DurationMs)
            .HasColumnName("duration_ms")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasOne<E_POS.Domain.Modules.Integration.Entities.IntegrationProvider>()
            .WithMany()
            .HasForeignKey(x => x.IntegrationProviderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integration_request_logs_df4e0286");

        builder.HasOne<E_POS.Domain.Modules.Integration.Entities.PlatformIntegration>()
            .WithMany()
            .HasForeignKey(x => x.PlatformIntegrationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integration_request_logs_c3a27762");

        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integration_request_logs_210d3375");
        // <second-brain-checks>
        builder.ToTable(t => t.HasCheckConstraint("ck_platform_integration_request_logs_af20525d", "duration_ms IS NULL OR duration_ms >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_platform_integration_request_logs_a2ce2e91", "response_status_code IS NULL OR response_status_code >= 100"));
        // </second-brain-checks>

        // </second-brain-constraints>
    }
}