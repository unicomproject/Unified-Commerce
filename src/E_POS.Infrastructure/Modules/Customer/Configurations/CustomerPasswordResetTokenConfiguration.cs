using E_POS.Domain.Modules.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.Customer.Entities.Customer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Customer.Configurations;

public sealed class CustomerPasswordResetTokenConfiguration : IEntityTypeConfiguration<CustomerPasswordResetToken>
{
    public void Configure(EntityTypeBuilder<CustomerPasswordResetToken> builder)
    {
        builder.ToTable("customer_password_reset_tokens");

        builder.HasKey(x => x.Id).HasName("pk_customer_password_reset_tokens");

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

        builder.Property(x => x.CustomerAuthAccountId)
            .HasColumnName("customer_auth_account_id")
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.VerifiedOtpId)
            .HasColumnName("verified_otp_id")
            .IsRequired(false);

        builder.HasOne<CustomerAuthAccount>()
            .WithMany()
            .HasForeignKey(x => x.CustomerAuthAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_password_reset_tokens_customer_auth_account_id_customer_auth_accounts");

        builder.HasOne<CustomerVerificationOtp>()
            .WithMany()
            .HasForeignKey(x => x.VerifiedOtpId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_password_reset_tokens_verified_otp_id_customer_verification_otps");

        builder.HasIndex(x => new { x.TenantId, x.TokenHash })
            .IsUnique()
            .HasDatabaseName("uq_customer_password_reset_tokens_tenant_id_token_hash");
    }
}


