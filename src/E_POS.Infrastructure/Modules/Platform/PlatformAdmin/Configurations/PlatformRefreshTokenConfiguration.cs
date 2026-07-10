using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Configurations;

public sealed class PlatformRefreshTokenConfiguration : IEntityTypeConfiguration<PlatformRefreshToken>
{
    public void Configure(EntityTypeBuilder<PlatformRefreshToken> builder)
    {
        builder.ToTable("platform_refresh_tokens");

        builder.HasKey(x => x.Id).HasName("pk_platform_refresh_tokens");

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

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.PlatformAuthSessionId)
            .HasColumnName("platform_auth_session_id")
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.PlatformUserId)
            .HasColumnName("platform_user_id")
            .IsRequired();

        builder.Property(x => x.TokenFamilyId)
            .HasColumnName("token_family_id")
            .IsRequired();

        builder.Property(x => x.ReplacedByTokenId)
            .HasColumnName("replaced_by_token_id");

        builder.Property(x => x.UsedAt)
            .HasColumnName("used_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RevokedByPlatformUserId)
            .HasColumnName("revoked_by_platform_user_id");

        builder.Property(x => x.RevokeReason)
            .HasColumnName("revoke_reason")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250);

        builder.HasOne<PlatformAuthSession>()
            .WithMany()
            .HasForeignKey(x => x.PlatformAuthSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_refresh_tokens_platform_auth_session_id_platform_auth_sessions");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_refresh_tokens_platform_user_id_platform_users");

        builder.HasOne<PlatformRefreshToken>()
            .WithMany()
            .HasForeignKey(x => x.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_refresh_tokens_replaced_by_token_id_platform_refresh_tokens");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.RevokedByPlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_refresh_tokens_revoked_by_platform_user_id_platform_users");

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("uq_platform_refresh_tokens_token_hash");

        builder.HasIndex(x => x.PlatformAuthSessionId)
            .IsUnique()
            .HasFilter("status = 'ACTIVE'")
            .HasDatabaseName("uq_platform_refresh_tokens_platform_auth_session_id_active");

        builder.HasIndex(x => new { x.PlatformAuthSessionId, x.Status })
            .HasDatabaseName("ix_platform_refresh_tokens_platform_auth_session_id_status");

        builder.HasIndex(x => x.TokenFamilyId)
            .HasDatabaseName("ix_platform_refresh_tokens_token_family_id");

        builder.HasIndex(x => x.TokenFamilyId)
            .IsUnique()
            .HasFilter("status = 'ACTIVE'")
            .HasDatabaseName("uq_platform_refresh_tokens_token_family_id_active");

        builder.HasIndex(x => x.ReplacedByTokenId)
            .IsUnique()
            .HasFilter("replaced_by_token_id IS NOT NULL")
            .HasDatabaseName("uq_platform_refresh_tokens_replaced_by_token_id");

        builder.HasIndex(x => new { x.PlatformUserId, x.Status })
            .HasDatabaseName("ix_platform_refresh_tokens_platform_user_id_status");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_platform_refresh_tokens_status",
                "status IN ('ACTIVE', 'USED', 'EXPIRED', 'REVOKED')");
            t.HasCheckConstraint(
                "ck_platform_refresh_tokens_expires_at_created_at",
                "expires_at > created_at");
        });
    }
}
