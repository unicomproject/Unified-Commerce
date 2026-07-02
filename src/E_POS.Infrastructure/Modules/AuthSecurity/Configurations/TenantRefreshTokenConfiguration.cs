using E_POS.Domain.Modules.AuthSecurity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.AuthSecurity.Configurations;

public sealed class TenantRefreshTokenConfiguration : IEntityTypeConfiguration<TenantRefreshToken>
{
    public void Configure(EntityTypeBuilder<TenantRefreshToken> builder)
    {
        builder.ToTable("tenant_refresh_tokens");

        builder.HasKey(x => x.Id).HasName("pk_tenant_refresh_tokens");

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

        builder.Property(x => x.TenantAuthSessionId)
            .HasColumnName("tenant_auth_session_id")
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<TenantAuthSession>()
            .WithMany()
            .HasForeignKey(x => x.TenantAuthSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_refresh_tokens_tenant_auth_session_id_tenant_auth_sessions");

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("uq_tenant_refresh_tokens_token_hash");

        builder.HasIndex(x => new { x.TenantAuthSessionId, x.Status })
            .HasDatabaseName("ix_tenant_refresh_tokens_tenant_auth_session_id_status");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_tenant_refresh_tokens_status", "status IN ('ACTIVE', 'USED', 'EXPIRED', 'REVOKED')");
            t.HasCheckConstraint("ck_tenant_refresh_tokens_expires_at_created_at", "expires_at > created_at");
        });
    }
}