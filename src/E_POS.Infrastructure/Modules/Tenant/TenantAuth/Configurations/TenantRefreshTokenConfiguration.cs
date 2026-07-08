using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.TenantAuth.Configurations;

public sealed class TenantRefreshTokenConfiguration : IEntityTypeConfiguration<TenantRefreshToken>
{
    public void Configure(EntityTypeBuilder<TenantRefreshToken> builder)
    {
        builder.ToTable("tenant_refresh_tokens");

        builder.HasKey(x => x.Id).HasName("pk_tenant_refresh_tokens");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.TenantAuthSessionId).HasColumnName("tenant_auth_session_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.TokenHash).HasColumnName("token_hash").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(x => x.TokenFamilyId).HasColumnName("token_family_id").IsRequired();
        builder.Property(x => x.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
        builder.Property(x => x.UsedAt).HasColumnName("used_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.RevokedByTenantUserId).HasColumnName("revoked_by_tenant_user_id");
        builder.Property(x => x.RevokedByPlatformUserId).HasColumnName("revoked_by_platform_user_id");
        builder.Property(x => x.RevokeReason).HasColumnName("revoke_reason").HasColumnType("varchar(250)").HasMaxLength(250);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_refresh_tokens_tenant_id_tenants");

        builder.HasOne<TenantAuthSession>()
            .WithMany()
            .HasForeignKey(x => x.TenantAuthSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_refresh_tokens_tenant_auth_session_id_tenant_auth_sessions");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_refresh_tokens_user_id_tenant_users");

        builder.HasOne<TenantRefreshToken>()
            .WithMany()
            .HasForeignKey(x => x.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_refresh_tokens_replaced_by_token_id_tenant_refresh_tokens");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.RevokedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_refresh_tokens_revoked_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("uq_tenant_refresh_tokens_token_hash");

        builder.HasIndex(x => x.ReplacedByTokenId)
            .HasFilter("replaced_by_token_id IS NOT NULL")
            .IsUnique()
            .HasDatabaseName("uq_tenant_refresh_tokens_replaced_by_token_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_tenant_refresh_tokens_revoked_by", "NOT (revoked_by_tenant_user_id IS NOT NULL AND revoked_by_platform_user_id IS NOT NULL)");
        });
    }
}

