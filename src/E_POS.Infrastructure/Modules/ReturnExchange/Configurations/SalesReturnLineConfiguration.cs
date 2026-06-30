using E_POS.Domain.Modules.Orders.Entities;
using E_POS.Domain.Modules.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ReturnExchange.Configurations;

public sealed class SalesReturnLineConfiguration : IEntityTypeConfiguration<SalesReturnLine>
{
    public void Configure(EntityTypeBuilder<SalesReturnLine> builder)
    {
        builder.ToTable("sales_return_lines");

        builder.HasKey(x => x.Id).HasName("pk_sales_return_lines");

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

        builder.Property(x => x.RequestedQuantity)
            .HasColumnName("requested_quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.SalesOrderLineId)
            .HasColumnName("sales_order_line_id")
            .IsRequired(false);

        builder.Property(x => x.SalesReturnId)
            .HasColumnName("sales_return_id")
            .IsRequired(false);

        builder.HasOne<SalesReturn>()
            .WithMany()
            .HasForeignKey(x => x.SalesReturnId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_return_lines_sales_return_id_sales_returns");

        builder.HasOne<SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_return_lines_sales_order_line_id_sales_order_lines");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_return_lines_requested_quantity", "requested_quantity > 0")); 
    }
}

