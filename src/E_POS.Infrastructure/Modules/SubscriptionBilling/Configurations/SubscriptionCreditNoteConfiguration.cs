using E_POS.Domain.Modules.SubscriptionBilling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.SubscriptionBilling.Configurations;

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

        builder.HasOne<SubscriptionInvoice>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionInvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_credit_notes_subscription_invoice_id_subscription_invoices");

        builder.HasIndex(x => new { x.TenantId, x.CreditNoteNumber })
            .IsUnique()
            .HasDatabaseName("uq_subscription_credit_notes_tenant_id_credit_note_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_credit_notes_total_credit_amount", "total_credit_amount >= 0")); 
    }
}

