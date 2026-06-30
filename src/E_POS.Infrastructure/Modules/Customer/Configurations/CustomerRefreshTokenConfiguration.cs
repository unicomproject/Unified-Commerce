using E_POS.Domain.Modules.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.Customer.Entities.Customer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Customer.Configurations;

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
            .HasColumnName("tenant_id");

        builder.Property(x => x.CustomerAuthSessionId)
            .HasColumnName("customer_auth_session_id")
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.HasOne<CustomerAuthSession>()
            .WithMany()
            .HasForeignKey(x => x.CustomerAuthSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_refresh_tokens_customer_auth_session_id_customer_auth_sessions");

        builder.HasIndex(x => new { x.TenantId, x.TokenHash })
            .IsUnique()
            .HasDatabaseName("uq_customer_refresh_tokens_tenant_id_token_hash");
        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_customer_refresh_tokens_tenant_id_id");
    }
}



