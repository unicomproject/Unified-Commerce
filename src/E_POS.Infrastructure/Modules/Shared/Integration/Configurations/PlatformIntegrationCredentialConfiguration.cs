using E_POS.Domain.Modules.Shared.Integration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.Integration.Configurations;

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

        builder.Property(x => x.PlatformIntegrationId)
            .HasColumnName("platform_integration_id")
            .IsRequired();

        builder.Property(x => x.CredentialName)
            .HasColumnName("credential_name")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.CredentialType)
            .HasColumnName("credential_type")
            .IsRequired();

        builder.Property(x => x.EncryptedValue)
            .HasColumnName("encrypted_value")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.EncryptionKeyId)
            .HasColumnName("encryption_key_id")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.CredentialKeyVersion)
            .HasColumnName("credential_key_version")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired(false);

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.LastRotatedAt)
            .HasColumnName("last_rotated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.PlatformIntegrationId, x.CredentialName })
            .IsUnique()
            .HasDatabaseName("ux_platform_integration_credentials_f64f2b19");

        builder.HasOne<E_POS.Domain.Modules.Shared.Integration.Entities.PlatformIntegration>()
            .WithMany()
            .HasForeignKey(x => x.PlatformIntegrationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integration_credentials_308650a2");

        builder.HasOne<E_POS.Domain.Modules.Platform.PlatformAdmin.Entities.PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByPlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_integration_credentials_fd56824e");
        // </second-brain-constraints>
    }
}

