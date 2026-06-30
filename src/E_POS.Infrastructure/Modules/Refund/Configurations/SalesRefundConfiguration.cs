using E_POS.Domain.Modules.Orders.Entities;
using E_POS.Domain.Modules.Refund.Entities;
using E_POS.Domain.Modules.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Refund.Configurations;

public sealed class SalesRefundConfiguration : IEntityTypeConfiguration<SalesRefund>
{
    public void Configure(EntityTypeBuilder<SalesRefund> builder)
    {
        builder.ToTable("sales_refunds");

        builder.HasKey(x => x.Id).HasName("pk_sales_refunds");

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

        builder.Property(x => x.RefundNumber)
            .HasColumnName("refund_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.RefundedAmount)
            .HasColumnName("refunded_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.RequestedAmount)
            .HasColumnName("requested_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired(false);

        builder.Property(x => x.SalesReturnId)
            .HasColumnName("sales_return_id")
            .IsRequired(false);

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refunds_sales_order_id_sales_orders");

        builder.HasOne<SalesReturn>()
            .WithMany()
            .HasForeignKey(x => x.SalesReturnId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refunds_sales_return_id_sales_returns");

        builder.HasIndex(x => new { x.TenantId, x.RefundNumber })
            .IsUnique()
            .HasDatabaseName("uq_sales_refunds_tenant_id_refund_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_refunds_requested_amount", "requested_amount >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_refunds_refunded_amount", "refunded_amount >= 0")); 
    }
}

