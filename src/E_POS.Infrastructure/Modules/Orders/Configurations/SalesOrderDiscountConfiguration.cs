using E_POS.Domain.Modules.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Orders.Configurations;

public sealed class SalesOrderDiscountConfiguration : IEntityTypeConfiguration<SalesOrderDiscount>
{
    public void Configure(EntityTypeBuilder<SalesOrderDiscount> builder)
    {
        builder.ToTable("sales_order_discounts");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_discounts");

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

        builder.Property(x => x.ApplicationSequence)
            .HasColumnName("application_sequence");

        builder.Property(x => x.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired(false);

        builder.Property(x => x.SalesOrderLineId)
            .HasColumnName("sales_order_line_id")
            .IsRequired(false);

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_discounts_sales_order_id_sales_orders");

        builder.HasOne<SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_discounts_sales_order_line_id_sales_order_lines");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_order_discounts_discount_amount", "discount_amount >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_order_discounts_application_sequence", "application_sequence > 0")); 
    }
}

