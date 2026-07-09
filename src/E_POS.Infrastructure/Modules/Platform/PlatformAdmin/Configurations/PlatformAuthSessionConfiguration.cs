using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Configurations;

public sealed class PlatformAuthSessionConfiguration : IEntityTypeConfiguration<PlatformAuthSession>
{
    public void Configure(EntityTypeBuilder<PlatformAuthSession> builder)
    {
        builder.ToTable("platform_auth_sessions");

        builder.HasKey(x => x.Id).HasName("pk_platform_auth_sessions");

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

        builder.Property(x => x.PlatformUserId)
            .HasColumnName("platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.SessionTokenHash)
            .HasColumnName("session_token_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("varchar(45)");

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasColumnType("text");

        builder.Property(x => x.DeviceName)
            .HasColumnName("device_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.LastSeenAt)
            .HasColumnName("last_seen_at")
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

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_auth_sessions_platform_user_id_platform_users");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.RevokedByPlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_auth_sessions_revoked_by_platform_user_id_platform_users");

        builder.HasIndex(x => x.SessionTokenHash)
            .IsUnique()
            .HasDatabaseName("uq_platform_auth_sessions_session_token_hash");

        builder.HasIndex(x => new { x.PlatformUserId, x.RevokedAt })
            .HasDatabaseName("ix_platform_auth_sessions_platform_user_id_revoked_at");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_platform_auth_sessions_status",
            "status IN ('ACTIVE', 'EXPIRED', 'REVOKED')"));
    }
}
