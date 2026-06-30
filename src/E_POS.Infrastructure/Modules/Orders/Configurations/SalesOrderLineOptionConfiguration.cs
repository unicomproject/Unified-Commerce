using E_POS.Domain.Modules.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Orders.Configurations;

public sealed class SalesOrderLineOptionConfiguration : IEntityTypeConfiguration<SalesOrderLineOption>
{
    public void Configure(EntityTypeBuilder<SalesOrderLineOption> builder)
    {
        builder.ToTable("sales_order_line_options");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_line_options");

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

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.SalesOrderLineId)
            .HasColumnName("sales_order_line_id")
            .IsRequired(false);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order");

        builder.HasOne<SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_options_sales_order_line_id_sales_order_lines");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_order_line_options_quantity", "quantity > 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_order_line_options_sort_order", "sort_order >= 0")); 
    }
}

