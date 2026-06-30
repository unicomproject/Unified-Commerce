using E_POS.Domain.Modules.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.Customer.Entities.Customer;
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
            .HasColumnName("tenant_id");

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired(false);

        builder.Property(x => x.FailedLoginCount)
            .HasColumnName("failed_login_count");

        builder.HasOne<CustomerEntity>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_auth_accounts_customer_id_customers");

        builder.HasIndex(x => new { x.TenantId, x.CustomerId })
            .IsUnique()
            .HasDatabaseName("uq_customer_auth_accounts_tenant_id_customer_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_customer_auth_accounts_failed_login_count", "failed_login_count >= 0")); 
    }
}


