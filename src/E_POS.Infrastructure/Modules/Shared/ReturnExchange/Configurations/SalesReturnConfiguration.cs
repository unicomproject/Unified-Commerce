using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Configurations;

public sealed class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.ToTable("sales_returns", t =>
        {
            t.HasCheckConstraint(
                "ck_sales_returns_total_requested_qty",
                "total_requested_qty >= 0");
            t.HasCheckConstraint(
                "ck_sales_returns_total_received_qty",
                "total_received_qty >= 0");
            t.HasCheckConstraint(
                "ck_sales_returns_total_refund_amount",
                "total_refund_amount >= 0");
            t.HasCheckConstraint(
                "ck_sales_returns_total_exchange_amount",
                "total_exchange_amount >= 0");
            t.HasCheckConstraint(
                "ck_sales_returns_return_status",
                "return_status IN ('REQUESTED', 'APPROVED', 'RECEIVED', 'COMPLETED', 'CANCELLED', 'REJECTED')");
            t.HasCheckConstraint(
                "ck_sales_returns_return_channel",
                "return_channel IN ('POS', 'ONLINE', 'PHONE', 'OTHER')");
        });

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
            .IsRequired(false);

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

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired(false);

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id")
            .IsRequired(false);

        builder.Property(x => x.ProcessingOutletCodeSnapshot)
            .HasColumnName("processing_outlet_code_snapshot")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.ProcessingOutletNameSnapshot)
            .HasColumnName("processing_outlet_name_snapshot")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(x => x.ReturnReasonId)
            .HasColumnName("return_reason_id")
            .IsRequired(false);

        builder.Property(x => x.ReturnReasonCodeSnapshot)
            .HasColumnName("return_reason_code_snapshot")
            .HasColumnType("varchar(60)")
            .HasMaxLength(60)
            .IsRequired(false);

        builder.Property(x => x.ReturnReasonNameSnapshot)
            .HasColumnName("return_reason_name_snapshot")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(x => x.ReturnNumber)
            .HasColumnName("return_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.ReturnChannel)
            .HasColumnName("return_channel")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ReturnStatus)
            .HasColumnName("return_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.RequestedAt)
            .HasColumnName("requested_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ApprovedAt)
            .HasColumnName("approved_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ReceivedAt)
            .HasColumnName("received_at")
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

        builder.Property(x => x.TotalRequestedQty)
            .HasColumnName("total_requested_qty")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.TotalReceivedQty)
            .HasColumnName("total_received_qty")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.TotalApprovedQty)
            .HasColumnName("total_approved_qty")
            .HasPrecision(18, 4)
            .IsRequired(false);

        builder.Property(x => x.TotalRefundAmount)
            .HasColumnName("total_refund_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.TotalExchangeAmount)
            .HasColumnName("total_exchange_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.ReturnNumber })
            .IsUnique()
            .HasDatabaseName("ux_sales_returns_35ae5e87");

        builder.HasIndex(x => new { x.TenantId, x.ReturnStatus, x.CompletedAt })
            .HasDatabaseName("ix_sales_returns_tenant_status_completed_at");

        builder.HasIndex(x => new { x.TenantId, x.SalesOrderId })
            .HasDatabaseName("ix_sales_returns_tenant_sales_order_id");

        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL")
            .HasDatabaseName("ux_sales_returns_tenant_idempotency_key");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_sales_returns_tenant_id_id");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_returns_06232f9d");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.DocumentNumberSequence>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.DocumentNumberSequenceId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_returns_document_number_sequence_tenant");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_returns_sales_order_tenant");

        builder.HasOne<E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.CustomerId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_returns_customer_tenant");

        builder.HasOne<E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities.Outlet>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.OutletId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_returns_outlet_tenant");

        builder.HasOne<E_POS.Domain.Modules.Shared.ReturnExchange.Entities.ReturnReason>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReturnReasonId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_returns_return_reason_tenant");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_returns_341b4dbd");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_returns_43c7eee4");
        // </second-brain-constraints>
    }
}
