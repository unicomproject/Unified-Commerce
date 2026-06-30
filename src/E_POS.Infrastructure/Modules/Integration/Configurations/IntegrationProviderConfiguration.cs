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
            .HasMaxLength(80);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.ProviderCategory)
            .HasColumnName("provider_category")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.HasIndex(x => x.ProviderCode)
            .IsUnique()
            .HasDatabaseName("uq_integration_providers_provider_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_integration_providers_provider_category", "provider_category IN ('PAYMENT', 'SMS', 'EMAIL', 'WHATSAPP', 'ACCOUNTING', 'DELIVERY', 'ANALYTICS', 'OTHER')")); 
    }
}

