using E_POS.Domain.Modules.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Orders.Configurations;

public sealed class SalesOrderChargeConfiguration : IEntityTypeConfiguration<SalesOrderCharge>
{
    public void Configure(EntityTypeBuilder<SalesOrderCharge> builder)
    {
        builder.ToTable("sales_order_charges");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_charges");

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

        builder.Property(x => x.ChargeAmount)
            .HasColumnName("charge_amount")
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
            .HasConstraintName("fk_sales_order_charges_sales_order_id_sales_orders");

        builder.HasOne<SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_charges_sales_order_line_id_sales_order_lines");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_order_charges_charge_amount", "charge_amount >= 0")); 
    }
}

