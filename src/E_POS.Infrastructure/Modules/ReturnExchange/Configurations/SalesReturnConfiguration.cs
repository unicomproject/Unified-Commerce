using E_POS.Domain.Modules.Orders.Entities;
using E_POS.Domain.Modules.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ReturnExchange.Configurations;

public sealed class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.ToTable("sales_returns");

        builder.HasKey(x => x.Id).HasName("pk_sales_returns");

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

        builder.Property(x => x.ReturnNumber)
            .HasColumnName("return_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired(false);

        builder.Property(x => x.TotalRefundAmount)
            .HasColumnName("total_refund_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalReturnAmount)
            .HasColumnName("total_return_amount")
            .HasPrecision(18, 2);

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_returns_sales_order_id_sales_orders");

        builder.HasIndex(x => new { x.TenantId, x.ReturnNumber })
            .IsUnique()
            .HasDatabaseName("uq_sales_returns_tenant_id_return_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_returns_total_return_amount", "total_return_amount >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_returns_total_refund_amount", "total_refund_amount >= 0")); 
    }
}

