using E_POS.Domain.Modules.SubscriptionBilling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.SubscriptionBilling.Configurations;

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

        builder.HasOne<SubscriptionInvoice>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionInvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_payment_links_subscription_invoice_id_subscription_invoices");

        builder.HasIndex(x => x.PaymentLinkTokenHash)
            .IsUnique()
            .HasDatabaseName("uq_subscription_payment_links_payment_link_token_hash");

        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_payment_links_expires_at_created_at", "expires_at IS NULL OR expires_at > created_at")); 
    }
}

