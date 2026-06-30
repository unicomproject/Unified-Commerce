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

        builder.Property(x => x.IntegrationCode)
            .HasColumnName("integration_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.IntegrationProviderId)
            .HasColumnName("integration_provider_id")
            .IsRequired();

        builder.HasOne<IntegrationProvider>()
            .WithMany()
            .HasForeignKey(x => x.IntegrationProviderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integrations_integration_provider_id_integration_providers");

        builder.HasIndex(x => x.IntegrationCode)
            .IsUnique()
            .HasDatabaseName("uq_platform_integrations_integration_code");
    }
}

