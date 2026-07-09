using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionInvoiceLineConfiguration : IEntityTypeConfiguration<SubscriptionInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SubscriptionInvoiceLine> builder)
    {
        builder.ToTable("subscription_invoice_lines");

        builder.HasKey(x => x.Id).HasName("pk_subscription_invoice_lines");

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

        builder.Property(x => x.LineNumber)
            .HasColumnName("line_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.LineTotalAmount)
            .HasColumnName("line_total_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.SubscriptionInvoiceId)
            .HasColumnName("subscription_invoice_id")
            .IsRequired();

        builder.Property(x => x.InvoiceId)
            .HasColumnName("invoice_id");

        builder.Property(x => x.LineNumberInt)
            .HasColumnName("line_number_int");

        builder.Property(x => x.ItemType)
            .HasColumnName("item_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.ItemReferenceId)
            .HasColumnName("item_reference_id");

        builder.Property(x => x.ItemCode)
            .HasColumnName("item_code")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120);

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250);

        builder.Property(x => x.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(18, 4);

        builder.Property(x => x.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasPrecision(18, 4);

        builder.Property(x => x.TaxAmount)
            .HasColumnName("tax_amount")
            .HasPrecision(18, 4);

        builder.Property(x => x.LineTotal)
            .HasColumnName("line_total")
            .HasPrecision(18, 4);

        builder.HasOne<SubscriptionInvoice>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionInvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_invoice_lines_subscription_invoice_id_subscription_invoices");

        builder.HasOne<SubscriptionInvoice>()
            .WithMany()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_invoice_lines_invoice_id_subscription_invoices");

        builder.HasIndex(x => new { x.SubscriptionInvoiceId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("uq_subscription_invoice_lines_subscription_invoice_id_line_number");

        builder.HasIndex(x => x.InvoiceId)
            .HasDatabaseName("ix_subscription_invoice_lines_invoice_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_subscription_invoice_lines_quantity", "quantity > 0");
            t.HasCheckConstraint("ck_subscription_invoice_lines_line_total_amount", "line_total_amount >= 0");
            t.HasCheckConstraint("ck_subscription_invoice_lines_unit_price", "unit_price IS NULL OR unit_price >= 0");
            t.HasCheckConstraint("ck_subscription_invoice_lines_discount_amount", "discount_amount IS NULL OR discount_amount >= 0");
            t.HasCheckConstraint("ck_subscription_invoice_lines_tax_amount", "tax_amount IS NULL OR tax_amount >= 0");
            t.HasCheckConstraint("ck_subscription_invoice_lines_line_total", "line_total IS NULL OR line_total >= 0");
        });
    }
}
