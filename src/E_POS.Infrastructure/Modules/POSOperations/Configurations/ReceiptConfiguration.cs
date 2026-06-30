using E_POS.Domain.Modules.Orders.Entities;
using E_POS.Domain.Modules.POSOperations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.POSOperations.Configurations;

public sealed class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("receipts");

        builder.HasKey(x => x.Id).HasName("pk_receipts");

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

        builder.Property(x => x.DocumentNumberSequenceId)
            .HasColumnName("document_number_sequence_id")
            .IsRequired();

        builder.Property(x => x.ReceiptNumber)
            .HasColumnName("receipt_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired(false);

        builder.Property(x => x.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 2);

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipts_sales_order_id_sales_orders");

        builder.HasOne<DocumentNumberSequence>()
            .WithMany()
            .HasForeignKey(x => x.DocumentNumberSequenceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipts_document_number_sequence_id_document_number_sequences");

        builder.HasIndex(x => new { x.TenantId, x.ReceiptNumber })
            .IsUnique()
            .HasDatabaseName("uq_receipts_tenant_id_receipt_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_receipts_total_amount", "total_amount >= 0")); 
    }
}

