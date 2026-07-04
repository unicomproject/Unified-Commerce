using E_POS.Domain.Modules.Integration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Integration.Configurations;

public sealed class IntegrationProviderConfiguration : IEntityTypeConfiguration<IntegrationProvider>
{
    public void Configure(EntityTypeBuilder<IntegrationProvider> builder)
    {
        builder.ToTable("integration_providers");

        builder.HasKey(x => x.Id).HasName("pk_integration_providers");

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

        builder.Property(x => x.ProviderCode)
            .HasColumnName("provider_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.ProviderName)
            .HasColumnName("provider_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.ProviderCategory)
            .HasColumnName("provider_category")
            .IsRequired();

        builder.Property(x => x.ProviderType)
            .HasColumnName("provider_type")
            .IsRequired();

        builder.Property(x => x.AuthType)
            .HasColumnName("auth_type")
            .IsRequired();

        builder.Property(x => x.ApiBaseUrl)
            .HasColumnName("api_base_url")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.DocumentationUrl)
            .HasColumnName("documentation_url")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.SupportsWebhook)
            .HasColumnName("supports_webhook")
            .IsRequired();

        builder.Property(x => x.SupportsTestMode)
            .HasColumnName("supports_test_mode")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        // <second-brain-constraints>
        builder.HasIndex(x => x.ProviderCode)
            .IsUnique()
            .HasDatabaseName("ux_integration_providers_b845f32a");
        // </second-brain-constraints>
    }
}