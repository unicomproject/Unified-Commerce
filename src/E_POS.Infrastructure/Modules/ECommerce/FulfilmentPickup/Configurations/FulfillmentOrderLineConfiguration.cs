using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.FulfilmentPickup.Configurations;

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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.FulfillmentOrderId)
            .HasColumnName("fulfillment_order_id")
            .IsRequired();

        builder.Property(x => x.SalesOrderLineId)
            .HasColumnName("sales_order_line_id")
            .IsRequired();

        builder.Property(x => x.SalesOrderLineComponentId)
            .HasColumnName("sales_order_line_component_id")
            .IsRequired(false);

        builder.Property(x => x.RequestedQuantity)
            .HasColumnName("requested_quantity")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.PickedQuantity)
            .HasColumnName("picked_quantity")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.PackedQuantity)
            .HasColumnName("packed_quantity")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.FulfilledQuantity)
            .HasColumnName("fulfilled_quantity")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.CancelledQuantity)
            .HasColumnName("cancelled_quantity")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.LineStatus)
            .HasColumnName("line_status")
            .IsRequired();

        builder.Property(x => x.PickedByTenantUserId)
            .HasColumnName("picked_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.PackedByTenantUserId)
            .HasColumnName("packed_by_tenant_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_order_lines_aff467be");

        builder.HasOne<E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities.FulfillmentOrder>()
            .WithMany()
            .HasForeignKey(x => x.FulfillmentOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_order_lines_5ac519a0");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_order_lines_f29543cf");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrderLineComponent>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderLineComponentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_order_lines_9a8cfc05");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.PickedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_order_lines_ec4817ec");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.PackedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_order_lines_bcaca9ef");
        // </second-brain-constraints>
    }
}

