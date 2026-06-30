using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class StockAdjustmentLineConfiguration : IEntityTypeConfiguration<StockAdjustmentLine>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentLine> builder)
    {
        builder.ToTable("stock_adjustment_lines");

        builder.HasKey(x => x.Id).HasName("pk_stock_adjustment_lines");

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

        builder.Property(x => x.AdjustmentQuantity)
            .HasColumnName("adjustment_quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.LineNumber)
            .HasColumnName("line_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.StockAdjustmentId)
            .HasColumnName("stock_adjustment_id")
            .IsRequired();

        builder.HasOne<StockAdjustment>()
            .WithMany()
            .HasForeignKey(x => x.StockAdjustmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stock_adjustment_lines_stock_adjustment_id_stock_adjustments");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stock_adjustment_lines_product_id_products");

        builder.HasIndex(x => new { x.StockAdjustmentId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("uq_stock_adjustment_lines_stock_adjustment_id_line_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_stock_adjustment_lines_adjustment_quantity", "adjustment_quantity <> 0")); 
    }
}

