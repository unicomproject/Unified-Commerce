using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.Customer.Configurations;

public sealed class CustomerRefreshTokenConfiguration : IEntityTypeConfiguration<CustomerRefreshToken>
{
    public void Configure(EntityTypeBuilder<CustomerRefreshToken> builder)
    {
        builder.ToTable("customer_refresh_tokens");

        builder.HasKey(x => x.Id).HasName("pk_customer_refresh_tokens");

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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.CustomerAuthSessionId)
            .HasColumnName("customer_auth_session_id")
            .IsRequired();

        builder.Property(x => x.TokenFamilyId)
            .HasColumnName("token_family_id")
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ReplacedByTokenId)
            .HasColumnName("replaced_by_token_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UsedAt)
            .HasColumnName("used_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RevokedReason)
            .HasColumnName("revoked_reason")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_refresh_tokens_tenant_id_tenants");

        builder.HasOne<CustomerAuthSession>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.CustomerAuthSessionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_refresh_tokens_customer_auth_session_id_customer_auth_sessions");

        builder.HasOne<CustomerRefreshToken>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReplacedByTokenId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_refresh_tokens_replaced_by_token_id_customer_refresh_tokens");

        builder.HasIndex(x => new { x.TenantId, x.TokenHash })
            .IsUnique()
            .HasDatabaseName("uq_customer_refresh_tokens_tenant_id_token_hash");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_customer_refresh_tokens_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_customer_refresh_tokens_status", "status IN ('ACTIVE', 'USED', 'EXPIRED', 'REVOKED')");
        });
    }
}


