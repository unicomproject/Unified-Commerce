using E_POS.Domain.Modules.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Orders.Configurations;

public sealed class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.ToTable("sales_order_lines");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_lines");

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

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired(false);

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_lines_sales_order_id_sales_orders");

        builder.HasIndex(x => new { x.SalesOrderId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("uq_sales_order_lines_sales_order_id_line_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_order_lines_quantity", "quantity > 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_order_lines_line_total_amount", "line_total_amount >= 0")); 
    }
}

