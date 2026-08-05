using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CustomerTenant = E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant;

namespace E_POS.Infrastructure.Modules.ECommerce.Customer.Configurations;

public sealed class CustomerExternalAuthAccountConfiguration : IEntityTypeConfiguration<CustomerExternalAuthAccount>
{
    public void Configure(EntityTypeBuilder<CustomerExternalAuthAccount> builder)
    {
        builder.ToTable("customer_external_auth_accounts");

        builder.HasKey(x => x.Id).HasName("pk_customer_external_auth_accounts");

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

        builder.Property(x => x.ProviderCode)
            .HasColumnName("provider_code")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ProviderSubject)
            .HasColumnName("provider_subject")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ProviderEmail)
            .HasColumnName("provider_email")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.ProviderEmailVerified)
            .HasColumnName("provider_email_verified")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.LinkedAt)
            .HasColumnName("linked_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.HasOne<CustomerTenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_external_auth_accounts_tenant_id_tenants");

        builder.HasOne<CustomerAuthAccount>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.CustomerAuthAccountId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_external_auth_accounts_auth_account");

        builder.HasIndex(x => new { x.TenantId, x.ProviderCode, x.ProviderSubject })
            .IsUnique()
            .HasDatabaseName("uq_customer_ext_auth_tenant_provider_subject");

        builder.HasIndex(x => new { x.TenantId, x.CustomerAuthAccountId, x.ProviderCode })
            .IsUnique()
            .HasDatabaseName("uq_customer_ext_auth_tenant_account_provider");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_customer_ext_auth_tenant_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_customer_ext_auth_provider_code", "provider_code IN ('GOOGLE')");
            t.HasCheckConstraint("ck_customer_ext_auth_provider_subject", "length(trim(provider_subject)) > 0");
            t.HasCheckConstraint("ck_customer_ext_auth_status", "status IN ('ACTIVE', 'DISABLED', 'DELETED')");
        });
    }
}