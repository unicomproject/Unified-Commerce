using E_POS.Domain.Modules.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.Customer.Entities.Customer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Customer.Configurations;

public sealed class CustomerVerificationOtpConfiguration : IEntityTypeConfiguration<CustomerVerificationOtp>
{
    public void Configure(EntityTypeBuilder<CustomerVerificationOtp> builder)
    {
        builder.ToTable("customer_verification_otps");

        builder.HasKey(x => x.Id).HasName("pk_customer_verification_otps");

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

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(x => x.AttemptCount)
            .HasColumnName("attempt_count");

        builder.Property(x => x.MaxAttempts)
            .HasColumnName("max_attempts");

        builder.Property(x => x.NormalizedRecipientValue)
            .HasColumnName("normalized_recipient_value")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.VerificationPurpose)
            .HasColumnName("verification_purpose")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.HasOne<CustomerEntity>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_verification_otps_customer_id_customers");

        builder.HasIndex(x => new { x.TenantId, x.VerificationPurpose, x.NormalizedRecipientValue })
            .IsUnique()
            .HasDatabaseName("uq_customer_verification_otps_tenant_id_verification_purpose_normalized_recipient_value")
            .HasFilter("status = 'PENDING'");

        builder.ToTable(t => t.HasCheckConstraint("ck_customer_verification_otps_attempt_count", "attempt_count >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_customer_verification_otps_max_attempts", "max_attempts > 0")); 
    }
}


