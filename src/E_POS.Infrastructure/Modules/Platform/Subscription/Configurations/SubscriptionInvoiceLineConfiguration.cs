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

        builder.HasOne<SubscriptionInvoice>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionInvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_invoice_lines_subscription_invoice_id_subscription_invoices");

        builder.HasIndex(x => new { x.SubscriptionInvoiceId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("uq_subscription_invoice_lines_subscription_invoice_id_line_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_invoice_lines_quantity", "quantity > 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_invoice_lines_line_total_amount", "line_total_amount >= 0")); 
    }
}



