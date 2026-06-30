using E_POS.Domain.Modules.PlatformAdministration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.PlatformAdministration.Configurations;

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

        builder.HasOne<PlatformAuthSession>()
            .WithMany()
            .HasForeignKey(x => x.PlatformAuthSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_refresh_tokens_platform_auth_session_id_platform_auth_sessions");

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("uq_platform_refresh_tokens_token_hash");

        builder.ToTable(t => t.HasCheckConstraint("ck_platform_refresh_tokens_status", "status IN ('ACTIVE', 'USED', 'EXPIRED', 'REVOKED')")); 
    }
}

