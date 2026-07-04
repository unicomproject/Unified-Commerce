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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.SalesRefundId)
            .HasColumnName("sales_refund_id")
            .IsRequired();

        builder.Property(x => x.OriginalSalesPaymentId)
            .HasColumnName("original_sales_payment_id")
            .IsRequired();

        builder.Property(x => x.RefundPaymentMethodId)
            .HasColumnName("refund_payment_method_id")
            .IsRequired();

        builder.Property(x => x.RefundTransactionId)
            .HasColumnName("refund_transaction_id")
            .IsRequired(false);

        builder.Property(x => x.AllocatedAmount)
            .HasColumnName("allocated_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.AllocationStatus)
            .HasColumnName("allocation_status")
            .IsRequired();

        builder.Property(x => x.ExternalReference)
            .HasColumnName("external_reference")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refund_payment_allocations_8f5df16a");

        builder.HasOne<E_POS.Domain.Modules.Refund.Entities.SalesRefund>()
            .WithMany()
            .HasForeignKey(x => x.SalesRefundId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refund_payment_allocations_d8eb2e60");

        builder.HasOne<E_POS.Domain.Modules.Payment.Entities.SalesPayment>()
            .WithMany()
            .HasForeignKey(x => x.OriginalSalesPaymentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refund_payment_allocations_b7012693");

        builder.HasOne<E_POS.Domain.Modules.Payment.Entities.PaymentMethod>()
            .WithMany()
            .HasForeignKey(x => x.RefundPaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refund_payment_allocations_71431eb5");

        builder.HasOne<E_POS.Domain.Modules.Payment.Entities.SalesPaymentTransaction>()
            .WithMany()
            .HasForeignKey(x => x.RefundTransactionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refund_payment_allocations_a46d161a");
        // <second-brain-checks>
        builder.ToTable(t => t.HasCheckConstraint("ck_sales_refund_payment_allocations_93be5f04", "allocated_amount > 0"));
        // </second-brain-checks>

        // </second-brain-constraints>
    }
}