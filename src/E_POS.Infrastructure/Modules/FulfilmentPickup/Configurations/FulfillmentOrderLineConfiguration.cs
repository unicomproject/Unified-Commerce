using E_POS.Domain.Modules.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.FulfilmentPickup.Configurations;

public sealed class FulfillmentOrderLineConfiguration : IEntityTypeConfiguration<FulfillmentOrderLine>
{
    public void Configure(EntityTypeBuilder<FulfillmentOrderLine> builder)
    {
        builder.ToTable("fulfillment_order_lines");

        builder.HasKey(x => x.Id).HasName("pk_fulfillment_order_lines");

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

        builder.Property(x => x.FulfillmentOrderId)
            .HasColumnName("fulfillment_order_id")
            .IsRequired();

        builder.Property(x => x.RequestedQuantity)
            .HasColumnName("requested_quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.SalesOrderLineId)
            .HasColumnName("sales_order_line_id")
            .IsRequired(false);

        builder.HasOne<FulfillmentOrder>()
            .WithMany()
            .HasForeignKey(x => x.FulfillmentOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_order_lines_fulfillment_order_id_fulfillment_orders");

        builder.HasOne<SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_order_lines_sales_order_line_id_sales_order_lines");

        builder.ToTable(t => t.HasCheckConstraint("ck_fulfillment_order_lines_requested_quantity", "requested_quantity > 0")); 
    }
}

