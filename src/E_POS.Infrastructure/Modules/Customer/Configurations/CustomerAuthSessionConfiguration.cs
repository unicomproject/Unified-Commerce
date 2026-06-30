using E_POS.Domain.Modules.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.Customer.Entities.Customer;
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
            .HasColumnName("tenant_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CustomerAuthAccountId)
            .HasColumnName("customer_auth_account_id")
            .IsRequired();

        builder.Property(x => x.SessionTokenHash)
            .HasColumnName("session_token_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.HasOne<CustomerAuthAccount>()
            .WithMany()
            .HasForeignKey(x => x.CustomerAuthAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_auth_sessions_customer_auth_account_id_customer_auth_accounts");

        builder.HasIndex(x => new { x.TenantId, x.SessionTokenHash })
            .IsUnique()
            .HasDatabaseName("uq_customer_auth_sessions_tenant_id_session_token_hash");
    }
}


