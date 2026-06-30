using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class StockTransferStatusHistoryConfiguration : IEntityTypeConfiguration<StockTransferStatusHistory>
{
    public void Configure(EntityTypeBuilder<StockTransferStatusHistory> builder)
    {
        builder.ToTable("stock_transfer_status_history");

        builder.HasKey(x => x.Id).HasName("pk_stock_transfer_status_history");

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

        builder.Property(x => x.SequenceNumber)
            .HasColumnName("sequence_number");

        builder.Property(x => x.StockTransferId)
            .HasColumnName("stock_transfer_id")
            .IsRequired();

        builder.HasOne<StockTransfer>()
            .WithMany()
            .HasForeignKey(x => x.StockTransferId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stock_transfer_status_history_stock_transfer_id_stock_transfers");

        builder.HasIndex(x => new { x.StockTransferId, x.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("uq_stock_transfer_status_history_stock_transfer_id_sequence_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_stock_transfer_status_history_sequence_number", "sequence_number > 0")); 
    }
}

