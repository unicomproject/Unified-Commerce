using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionPaymentTransactionConfiguration : IEntityTypeConfiguration<SubscriptionPaymentTransaction>
{
    public void Configure(EntityTypeBuilder<SubscriptionPaymentTransaction> builder)
    {
        builder.ToTable("subscription_payment_transactions");

        builder.HasKey(x => x.Id).HasName("pk_subscription_payment_transactions");

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

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.ProviderTransactionReference)
            .HasColumnName("provider_transaction_reference")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.SubscriptionInvoiceId)
            .HasColumnName("subscription_invoice_id")
            .IsRequired();

        builder.Property(x => x.SubscriptionPaymentLinkId)
            .HasColumnName("subscription_payment_link_id")
            .IsRequired();

        builder.HasOne<SubscriptionInvoice>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionInvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_payment_transactions_subscription_invoice_id_subscription_invoices");

        builder.HasOne<SubscriptionPaymentLink>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionPaymentLinkId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_payment_transactions_subscription_payment_link_id_subscription_payment_links");

        builder.HasIndex(x => x.ProviderTransactionReference)
            .IsUnique()
            .HasDatabaseName("uq_subscription_payment_transactions_provider_transaction_reference");

        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_payment_transactions_amount", "amount >= 0")); 
    }
}



