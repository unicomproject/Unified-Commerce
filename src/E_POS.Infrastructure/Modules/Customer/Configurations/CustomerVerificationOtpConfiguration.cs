using E_POS.Domain.Modules.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.Customer.Entities.Customer;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
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
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(x => x.VerificationPurpose)
            .HasColumnName("verification_purpose")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.DeliveryChannel)
            .HasColumnName("delivery_channel")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.RecipientValue)
            .HasColumnName("recipient_value")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.NormalizedRecipientValue)
            .HasColumnName("normalized_recipient_value")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.OtpHash)
            .HasColumnName("otp_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.MaxAttempts)
            .HasColumnName("max_attempts")
            .IsRequired()
            .HasDefaultValue(5);

        builder.Property(x => x.ResendCount)
            .HasColumnName("resend_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.SentAt)
            .HasColumnName("sent_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.LastSentAt)
            .HasColumnName("last_sent_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.VerifiedAt)
            .HasColumnName("verified_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.InvalidatedAt)
            .HasColumnName("invalidated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ProviderName)
            .HasColumnName("provider_name")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.ProviderMessageId)
            .HasColumnName("provider_message_id")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.RequestIpAddress)
            .HasColumnName("request_ip_address")
            .HasColumnType("inet");

        builder.Property(x => x.RequestUserAgent)
            .HasColumnName("request_user_agent")
            .HasColumnType("text");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_verification_otps_tenant_id_tenants");

        builder.HasOne<CustomerEntity>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.CustomerId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_verification_otps_customer_id_customers");

        builder.HasIndex(x => new { x.TenantId, x.VerificationPurpose, x.NormalizedRecipientValue })
            .IsUnique()
            .HasDatabaseName("uq_customer_verification_otps_tenant_id_verification_purpose_normalized_recipient_value")
            .HasFilter("status = 'PENDING'");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_customer_verification_otps_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_customer_verification_otps_attempt_count", "attempt_count >= 0");
            t.HasCheckConstraint("ck_customer_verification_otps_max_attempts", "max_attempts > 0");
            t.HasCheckConstraint("ck_customer_verification_otps_resend_count", "resend_count >= 0");
            t.HasCheckConstraint("ck_customer_verification_otps_status", "status IN ('PENDING', 'VERIFIED', 'EXPIRED', 'INVALIDATED', 'FAILED')");
            t.HasCheckConstraint("ck_customer_verification_otps_verification_purpose", "verification_purpose IN ('EMAIL_VERIFY', 'PHONE_VERIFY', 'PASSWORD_RESET', 'LOGIN_VERIFY')");
            t.HasCheckConstraint("ck_customer_verification_otps_delivery_channel", "delivery_channel IN ('EMAIL', 'SMS', 'WHATSAPP')");
            t.HasCheckConstraint("ck_customer_verification_otps_expires_at", "expires_at > sent_at");
        });
    }
}


