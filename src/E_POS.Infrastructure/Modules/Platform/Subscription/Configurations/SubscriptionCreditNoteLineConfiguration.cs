using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionCreditNoteLineConfiguration : IEntityTypeConfiguration<SubscriptionCreditNoteLine>
{
    public void Configure(EntityTypeBuilder<SubscriptionCreditNoteLine> builder)
    {
        builder.ToTable("subscription_credit_note_lines");

        builder.HasKey(x => x.Id).HasName("pk_subscription_credit_note_lines");

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

        builder.Property(x => x.LineCreditAmount)
            .HasColumnName("line_credit_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.LineNumber)
            .HasColumnName("line_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.SubscriptionCreditNoteId)
            .HasColumnName("subscription_credit_note_id")
            .IsRequired();

        builder.Property(x => x.CreditNoteId)
            .HasColumnName("credit_note_id");

        builder.Property(x => x.InvoiceLineId)
            .HasColumnName("invoice_line_id");

        builder.Property(x => x.LineNumberInt)
            .HasColumnName("line_number_int");

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250);

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.UnitAmount)
            .HasColumnName("unit_amount")
            .HasPrecision(18, 4);

        builder.Property(x => x.TaxAmount)
            .HasColumnName("tax_amount")
            .HasPrecision(18, 4);

        builder.Property(x => x.LineTotal)
            .HasColumnName("line_total")
            .HasPrecision(18, 4);

        builder.HasOne<SubscriptionCreditNote>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionCreditNoteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_credit_note_lines_subscription_credit_note_id_subscription_credit_notes");

        builder.HasOne<SubscriptionCreditNote>()
            .WithMany()
            .HasForeignKey(x => x.CreditNoteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_credit_note_lines_credit_note_id_subscription_credit_notes");

        builder.HasOne<SubscriptionInvoiceLine>()
            .WithMany()
            .HasForeignKey(x => x.InvoiceLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_credit_note_lines_invoice_line_id_subscription_invoice_lines");

        builder.HasIndex(x => new { x.SubscriptionCreditNoteId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("uq_subscription_credit_note_lines_subscription_credit_note_id_line_number");

        builder.HasIndex(x => x.CreditNoteId)
            .HasDatabaseName("ix_subscription_credit_note_lines_credit_note_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_subscription_credit_note_lines_line_credit_amount", "line_credit_amount >= 0");
            t.HasCheckConstraint("ck_subscription_credit_note_lines_quantity", "quantity IS NULL OR quantity > 0");
            t.HasCheckConstraint("ck_subscription_credit_note_lines_unit_amount", "unit_amount IS NULL OR unit_amount >= 0");
            t.HasCheckConstraint("ck_subscription_credit_note_lines_tax_amount", "tax_amount IS NULL OR tax_amount >= 0");
            t.HasCheckConstraint("ck_subscription_credit_note_lines_line_total", "line_total IS NULL OR line_total >= 0");
        });
    }
}
