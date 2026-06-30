using E_POS.Domain.Modules.Orders.Entities;
using E_POS.Domain.Modules.Payment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Payment.Configurations;

public sealed class SalesPaymentConfiguration : IEntityTypeConfiguration<SalesPayment>
{
    public void Configure(EntityTypeBuilder<SalesPayment> builder)
    {
        builder.ToTable("sales_payments");

        builder.HasKey(x => x.Id).HasName("pk_sales_payments");

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

        builder.Property(x => x.PaidAmount)
            .HasColumnName("paid_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.PaymentMethodId)
            .HasColumnName("payment_method_id")
            .IsRequired();

        builder.Property(x => x.PaymentNumber)
            .HasColumnName("payment_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.RequestedAmount)
            .HasColumnName("requested_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired(false);

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payments_sales_order_id_sales_orders");

        builder.HasOne<PaymentMethod>()
            .WithMany()
            .HasForeignKey(x => x.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payments_payment_method_id_payment_methods");

        builder.HasIndex(x => new { x.TenantId, x.PaymentNumber })
            .IsUnique()
            .HasDatabaseName("uq_sales_payments_tenant_id_payment_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_payments_requested_amount", "requested_amount >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_payments_paid_amount", "paid_amount >= 0")); 
    }
}

