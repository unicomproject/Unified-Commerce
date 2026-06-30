using E_POS.Domain.Modules.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.FulfilmentPickup.Configurations;

public sealed class FulfillmentOrderConfiguration : IEntityTypeConfiguration<FulfillmentOrder>
{
    public void Configure(EntityTypeBuilder<FulfillmentOrder> builder)
    {
        builder.ToTable("fulfillment_orders");

        builder.HasKey(x => x.Id).HasName("pk_fulfillment_orders");

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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.FulfillmentMethodOutletId)
            .HasColumnName("fulfillment_method_outlet_id")
            .IsRequired();

        builder.Property(x => x.FulfillmentNumber)
            .HasColumnName("fulfillment_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired(false);

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_orders_sales_order_id_sales_orders");

        builder.HasOne<FulfillmentMethodOutlet>()
            .WithMany()
            .HasForeignKey(x => x.FulfillmentMethodOutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_orders_fulfillment_method_outlet_id_fulfillment_method_outlets");

        builder.HasIndex(x => new { x.TenantId, x.FulfillmentNumber })
            .IsUnique()
            .HasDatabaseName("uq_fulfillment_orders_tenant_id_fulfillment_number");
    }
}

