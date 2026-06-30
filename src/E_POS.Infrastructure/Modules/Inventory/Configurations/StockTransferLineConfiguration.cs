using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class StockTransferLineConfiguration : IEntityTypeConfiguration<StockTransferLine>
{
    public void Configure(EntityTypeBuilder<StockTransferLine> builder)
    {
        builder.ToTable("stock_transfer_lines");

        builder.HasKey(x => x.Id).HasName("pk_stock_transfer_lines");

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

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.RequestedQuantity)
            .HasColumnName("requested_quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.StockTransferId)
            .HasColumnName("stock_transfer_id")
            .IsRequired();

        builder.HasOne<StockTransfer>()
            .WithMany()
            .HasForeignKey(x => x.StockTransferId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stock_transfer_lines_stock_transfer_id_stock_transfers");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stock_transfer_lines_product_id_products");

        builder.HasIndex(x => new { x.StockTransferId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("uq_stock_transfer_lines_stock_transfer_id_line_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_stock_transfer_lines_requested_quantity", "requested_quantity > 0")); 
    }
}

