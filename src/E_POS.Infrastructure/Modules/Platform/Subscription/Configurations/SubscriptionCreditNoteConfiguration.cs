using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionCreditNoteConfiguration : IEntityTypeConfiguration<SubscriptionCreditNote>
{
    public void Configure(EntityTypeBuilder<SubscriptionCreditNote> builder)
    {
        builder.ToTable("subscription_credit_notes");

        builder.HasKey(x => x.Id).HasName("pk_subscription_credit_notes");

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

        builder.Property(x => x.CreditNoteNumber)
            .HasColumnName("credit_note_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.SubscriptionInvoiceId)
            .HasColumnName("subscription_invoice_id")
            .IsRequired();

        builder.Property(x => x.TotalCreditAmount)
            .HasColumnName("total_credit_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.InvoiceId)
            .HasColumnName("invoice_id");

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasColumnType("text");

        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3);

        builder.Property(x => x.SubtotalAmount)
            .HasColumnName("subtotal_amount")
            .HasPrecision(18, 4);

        builder.Property(x => x.TaxAmount)
            .HasColumnName("tax_amount")
            .HasPrecision(18, 4);

        builder.Property(x => x.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 4);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.IssuedAt)
            .HasColumnName("issued_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.AppliedAt)
            .HasColumnName("applied_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id");

        builder.HasOne<SubscriptionInvoice>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionInvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_credit_notes_subscription_invoice_id_subscription_invoices");

        builder.HasOne<SubscriptionInvoice>()
            .WithMany()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_credit_notes_invoice_id_subscription_invoices");

        builder.HasOne<E_POS.Domain.Modules.Platform.PlatformAdmin.Entities.PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByPlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_credit_notes_created_by_platform_user_id_platform_users");

        builder.HasIndex(x => new { x.TenantId, x.CreditNoteNumber })
            .IsUnique()
            .HasDatabaseName("uq_subscription_credit_notes_tenant_id_credit_note_number");

        builder.HasIndex(x => x.InvoiceId)
            .HasDatabaseName("ix_subscription_credit_notes_invoice_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_subscription_credit_notes_total_credit_amount", "total_credit_amount >= 0");
            t.HasCheckConstraint("ck_subscription_credit_notes_subtotal_amount", "subtotal_amount IS NULL OR subtotal_amount >= 0");
            t.HasCheckConstraint("ck_subscription_credit_notes_tax_amount", "tax_amount IS NULL OR tax_amount >= 0");
            t.HasCheckConstraint("ck_subscription_credit_notes_total_amount", "total_amount IS NULL OR total_amount >= 0");
        });
    }
}
