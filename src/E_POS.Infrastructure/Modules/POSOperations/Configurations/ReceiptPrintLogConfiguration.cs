using E_POS.Domain.Modules.POSOperations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.POSOperations.Configurations;

public sealed class ReceiptPrintLogConfiguration : IEntityTypeConfiguration<ReceiptPrintLog>
{
    public void Configure(EntityTypeBuilder<ReceiptPrintLog> builder)
    {
        builder.ToTable("receipt_print_logs");

        builder.HasKey(x => x.Id).HasName("pk_receipt_print_logs");

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

        builder.Property(x => x.AttemptNumber)
            .HasColumnName("attempt_number");

        builder.Property(x => x.ReceiptId)
            .HasColumnName("receipt_id")
            .IsRequired();

        builder.HasOne<Receipt>()
            .WithMany()
            .HasForeignKey(x => x.ReceiptId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_print_logs_receipt_id_receipts");

        builder.HasIndex(x => new { x.ReceiptId, x.AttemptNumber })
            .IsUnique()
            .HasDatabaseName("uq_receipt_print_logs_receipt_id_attempt_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_receipt_print_logs_attempt_number", "attempt_number > 0")); 
    }
}

