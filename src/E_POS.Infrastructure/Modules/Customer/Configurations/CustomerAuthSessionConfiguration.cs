using E_POS.Domain.Modules.Customer.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Customer.Configurations;

public sealed class CustomerAuthSessionConfiguration : IEntityTypeConfiguration<CustomerAuthSession>
{
    public void Configure(EntityTypeBuilder<CustomerAuthSession> builder)
    {
        builder.ToTable("customer_auth_sessions");

        builder.HasKey(x => x.Id).HasName("pk_customer_auth_sessions");

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

        builder.Property(x => x.CustomerAuthAccountId)
            .HasColumnName("customer_auth_account_id")
            .IsRequired();

        builder.Property(x => x.SessionTokenHash)
            .HasColumnName("session_token_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("inet");

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasColumnType("text");

        builder.Property(x => x.DeviceName)
            .HasColumnName("device_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.LastActivityAt)
            .HasColumnName("last_activity_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

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
            .HasConstraintName("fk_customer_auth_sessions_tenant_id_tenants");

        builder.HasOne<CustomerAuthAccount>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.CustomerAuthAccountId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_auth_sessions_customer_auth_account_id_customer_auth_accounts");

        builder.HasIndex(x => new { x.TenantId, x.SessionTokenHash })
            .IsUnique()
            .HasDatabaseName("uq_customer_auth_sessions_tenant_id_session_token_hash");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_customer_auth_sessions_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_customer_auth_sessions_status", "status IN ('ACTIVE', 'EXPIRED', 'REVOKED')");
        });
    }
}


