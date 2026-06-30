using E_POS.Domain.Modules.Payment.Entities;
using E_POS.Domain.Modules.Refund.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Refund.Configurations;

public sealed class SalesRefundPaymentAllocationConfiguration : IEntityTypeConfiguration<SalesRefundPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<SalesRefundPaymentAllocation> builder)
    {
        builder.ToTable("sales_refund_payment_allocations");

        builder.HasKey(x => x.Id).HasName("pk_sales_refund_payment_allocations");

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

        builder.Property(x => x.AllocatedAmount)
            .HasColumnName("allocated_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.OriginalSalesPaymentId)
            .HasColumnName("original_sales_payment_id")
            .IsRequired();

        builder.Property(x => x.SalesRefundId)
            .HasColumnName("sales_refund_id")
            .IsRequired();

        builder.HasOne<SalesRefund>()
            .WithMany()
            .HasForeignKey(x => x.SalesRefundId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refund_payment_allocations_sales_refund_id_sales_refunds");

        builder.HasOne<SalesPayment>()
            .WithMany()
            .HasForeignKey(x => x.OriginalSalesPaymentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refund_payment_allocations_original_sales_payment_id_sales_payments");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_refund_payment_allocations_allocated_amount", "allocated_amount > 0")); 
    }
}

