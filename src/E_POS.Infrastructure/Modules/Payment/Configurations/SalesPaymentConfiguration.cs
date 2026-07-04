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
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.DocumentNumberSequenceId)
            .HasColumnName("document_number_sequence_id")
            .IsRequired(false);

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired();

        builder.Property(x => x.PaymentNumber)
            .HasColumnName("payment_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.PaymentMethodId)
            .HasColumnName("payment_method_id")
            .IsRequired();

        builder.Property(x => x.TillId)
            .HasColumnName("till_id")
            .IsRequired(false);

        builder.Property(x => x.TillSessionId)
            .HasColumnName("till_session_id")
            .IsRequired(false);

        builder.Property(x => x.PaymentStatus)
            .HasColumnName("payment_status")
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.RequestedAmount)
            .HasColumnName("requested_amount")
            .HasPrecision(18, 4)
            .IsRequired(false);

        builder.Property(x => x.TenderedAmount)
            .HasColumnName("tendered_amount")
            .HasPrecision(18, 4)
            .IsRequired(false);

        builder.Property(x => x.PaidAmount)
            .HasColumnName("paid_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.ChangeAmount)
            .HasColumnName("change_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.RefundedAmount)
            .HasColumnName("refunded_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.ExternalReference)
            .HasColumnName("external_reference")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.PaymentNote)
            .HasColumnName("payment_note")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.InitiatedAt)
            .HasColumnName("initiated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.PaidAt)
            .HasColumnName("paid_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.CancelledAt)
            .HasColumnName("cancelled_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.PaymentNumber })
            .IsUnique()
            .HasDatabaseName("ux_sales_payments_805b1537");

        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_sales_payments_3aae300c");

        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payments_3f9e9e72");

        builder.HasOne<E_POS.Domain.Modules.Orders.Entities.DocumentNumberSequence>()
            .WithMany()
            .HasForeignKey(x => x.DocumentNumberSequenceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payments_d4416ae5");

        builder.HasOne<E_POS.Domain.Modules.Orders.Entities.SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payments_4d0fbeda");

        builder.HasOne<E_POS.Domain.Modules.Payment.Entities.PaymentMethod>()
            .WithMany()
            .HasForeignKey(x => x.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payments_135a8ffe");

        builder.HasOne<E_POS.Domain.Modules.OutletTillDevice.Entities.Till>()
            .WithMany()
            .HasForeignKey(x => x.TillId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payments_8e5500f7");

        builder.HasOne<E_POS.Domain.Modules.HardwareCash.Entities.TillSession>()
            .WithMany()
            .HasForeignKey(x => x.TillSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payments_5f190cb1");

        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Currency>()
            .WithMany()
            .HasForeignKey(x => x.CurrencyCode)
            .HasPrincipalKey(x => x.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payments_f65f5c53");

        builder.HasOne<E_POS.Domain.Modules.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payments_860f8895");

        builder.HasOne<E_POS.Domain.Modules.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payments_fc091449");
        // <second-brain-checks>
        builder.ToTable(t => t.HasCheckConstraint("ck_sales_payments_e5c553d5", "requested_amount > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_sales_payments_5e2a8fc2", "refunded_amount <= paid_amount"));
        // </second-brain-checks>

        // </second-brain-constraints>
    }
}