using E_POS.Domain.Modules.Shared.Refund.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.Refund.Configurations;

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
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.DocumentNumberSequenceId)
            .HasColumnName("document_number_sequence_id")
            .IsRequired(false);

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired();

        builder.Property(x => x.SalesReturnId)
            .HasColumnName("sales_return_id")
            .IsRequired(false);

        builder.Property(x => x.RefundNumber)
            .HasColumnName("refund_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.RefundMode)
            .HasColumnName("refund_mode")
            .IsRequired();

        builder.Property(x => x.RefundStatus)
            .HasColumnName("refund_status")
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.RequestedAmount)
            .HasColumnName("requested_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.ApprovedAmount)
            .HasColumnName("approved_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.RefundedAmount)
            .HasColumnName("refunded_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.RefundReason)
            .HasColumnName("refund_reason")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.RequestedAt)
            .HasColumnName("requested_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ApprovedAt)
            .HasColumnName("approved_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at")
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

        builder.Property(x => x.ApprovedByTenantUserId)
            .HasColumnName("approved_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.RefundNumber })
            .IsUnique()
            .HasDatabaseName("ux_sales_refunds_bf2fab40");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refunds_29c32df7");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.DocumentNumberSequence>()
            .WithMany()
            .HasForeignKey(x => x.DocumentNumberSequenceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refunds_37ee683d");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refunds_3786682a");

        builder.HasOne<E_POS.Domain.Modules.Shared.ReturnExchange.Entities.SalesReturn>()
            .WithMany()
            .HasForeignKey(x => x.SalesReturnId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refunds_dded10a1");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Currency>()
            .WithMany()
            .HasForeignKey(x => x.CurrencyCode)
            .HasPrincipalKey(x => x.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refunds_5c636a91");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.ApprovedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refunds_bcb663c6");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refunds_1d9c0edc");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refunds_cc7855ab");
        // <second-brain-checks>
        builder.ToTable(t => t.HasCheckConstraint("ck_sales_refunds_2b7cb4b2", "requested_amount > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_sales_refunds_a3883de0", "approved_amount <= requested_amount"));
        builder.ToTable(t => t.HasCheckConstraint("ck_sales_refunds_732d4048", "refunded_amount <= approved_amount"));
        // </second-brain-checks>

        // </second-brain-constraints>
    }
}

