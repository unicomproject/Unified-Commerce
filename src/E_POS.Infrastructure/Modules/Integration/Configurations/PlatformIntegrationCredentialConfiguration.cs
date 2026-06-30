using E_POS.Domain.Modules.Integration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Integration.Configurations;

public sealed class PlatformIntegrationCredentialConfiguration : IEntityTypeConfiguration<PlatformIntegrationCredential>
{
    public void Configure(EntityTypeBuilder<PlatformIntegrationCredential> builder)
    {
        builder.ToTable("platform_integration_credentials");

        builder.HasKey(x => x.Id).HasName("pk_platform_integration_credentials");

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

        builder.Property(x => x.CredentialName)
            .HasColumnName("credential_name")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.PlatformIntegrationId)
            .HasColumnName("platform_integration_id")
            .IsRequired();

        builder.Property(x => x.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<PlatformIntegration>()
            .WithMany()
            .HasForeignKey(x => x.PlatformIntegrationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integration_credentials_platform_integration_id_platform_integrations");

        builder.HasIndex(x => new { x.PlatformIntegrationId, x.CredentialName })
            .IsUnique()
            .HasDatabaseName("uq_platform_integration_credentials_platform_integration_id_credential_name");

        builder.ToTable(t => t.HasCheckConstraint("ck_platform_integration_credentials_revoked_at_created_at", "revoked_at IS NULL OR revoked_at >= created_at")); 
    }
}

