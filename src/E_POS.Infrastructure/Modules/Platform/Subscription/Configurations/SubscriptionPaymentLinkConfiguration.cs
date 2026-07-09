using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionPaymentLinkConfiguration : IEntityTypeConfiguration<SubscriptionPaymentLink>
{
    public void Configure(EntityTypeBuilder<SubscriptionPaymentLink> builder)
    {
        builder.ToTable("subscription_payment_links");

        builder.HasKey(x => x.Id).HasName("pk_subscription_payment_links");

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

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.PaymentLinkTokenHash)
            .HasColumnName("payment_link_token_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.SubscriptionInvoiceId)
            .HasColumnName("subscription_invoice_id")
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(x => x.InvoiceId)
            .HasColumnName("invoice_id");

        builder.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.ProviderName)
            .HasColumnName("provider_name")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120);

        builder.Property(x => x.ProviderPaymentLinkId)
            .HasColumnName("provider_payment_link_id")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.PaymentUrl)
            .HasColumnName("payment_url")
            .HasColumnType("varchar(700)")
            .HasMaxLength(700);

        builder.Property(x => x.LinkStatus)
            .HasColumnName("link_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.SentToEmail)
            .HasColumnName("sent_to_email")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.SentAt)
            .HasColumnName("sent_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UsedAt)
            .HasColumnName("used_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.LastReminderAt)
            .HasColumnName("last_reminder_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ReminderCount)
            .HasColumnName("reminder_count")
            .HasDefaultValue(0);

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id");

        builder.HasOne<SubscriptionInvoice>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionInvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_payment_links_subscription_invoice_id_subscription_invoices");

        builder.HasOne<SubscriptionInvoice>()
            .WithMany()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_payment_links_invoice_id_subscription_invoices");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_payment_links_tenant_id_tenants");

        builder.HasOne<E_POS.Domain.Modules.Platform.PlatformAdmin.Entities.PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByPlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_payment_links_created_by_platform_user_id_platform_users");

        builder.HasIndex(x => x.PaymentLinkTokenHash)
            .IsUnique()
            .HasDatabaseName("uq_subscription_payment_links_payment_link_token_hash");

        builder.HasIndex(x => x.InvoiceId)
            .HasDatabaseName("ix_subscription_payment_links_invoice_id");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_subscription_payment_links_tenant_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_subscription_payment_links_expires_at_created_at", "expires_at IS NULL OR expires_at > created_at");
            t.HasCheckConstraint("ck_subscription_payment_links_reminder_count", "reminder_count IS NULL OR reminder_count >= 0");
        });
    }
}
