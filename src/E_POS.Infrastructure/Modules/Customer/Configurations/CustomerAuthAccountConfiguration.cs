using E_POS.Domain.Modules.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.Customer.Entities.Customer;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Customer.Configurations;

public sealed class CustomerAuthAccountConfiguration : IEntityTypeConfiguration<CustomerAuthAccount>
{
    public void Configure(EntityTypeBuilder<CustomerAuthAccount> builder)
    {
        builder.ToTable("customer_auth_accounts");

        builder.HasKey(x => x.Id).HasName("pk_customer_auth_accounts");

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

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.EmailVerifiedAt)
            .HasColumnName("email_verified_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.PhoneVerifiedAt)
            .HasColumnName("phone_verified_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.FailedLoginCount)
            .HasColumnName("failed_login_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastFailedLoginAt)
            .HasColumnName("last_failed_login_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.LockedUntil)
            .HasColumnName("locked_until")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.LastPasswordChangedAt)
            .HasColumnName("last_password_changed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_auth_accounts_tenant_id_tenants");

        builder.HasOne<CustomerEntity>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.CustomerId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_auth_accounts_customer_id_customers");

        builder.HasIndex(x => new { x.TenantId, x.CustomerId })
            .IsUnique()
            .HasDatabaseName("uq_customer_auth_accounts_tenant_id_customer_id");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_customer_auth_accounts_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_customer_auth_accounts_failed_login_count", "failed_login_count >= 0");
            t.HasCheckConstraint("ck_customer_auth_accounts_status", "status IN ('ACTIVE', 'LOCKED', 'DISABLED', 'DELETED')");
        });
    }
}
