using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Orders.Configurations;

public sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("sales_orders");

        builder.HasKey(x => x.Id).HasName("pk_sales_orders");

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
            .HasColumnName("document_number_sequence_id");

        builder.Property(x => x.OrderNumber)
            .HasColumnName("order_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.ExternalOrderReference)
            .HasColumnName("external_order_reference")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100);

        builder.Property(x => x.SalesChannelId)
            .HasColumnName("sales_channel_id")
            .IsRequired();

        builder.Property(x => x.OrderType)
            .HasColumnName("order_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.FulfillmentMethodOutletId)
            .HasColumnName("fulfillment_method_outlet_id");

        builder.Property(x => x.FulfillmentMethodCodeSnapshot)
            .HasColumnName("fulfillment_method_code_snapshot")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.RequestedCollectionAt)
            .HasColumnName("requested_collection_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RequestedCollectionEndAt)
            .HasColumnName("requested_collection_end_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CollectionTimezoneSnapshot)
            .HasColumnName("collection_timezone_snapshot")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.BusinessDate)
            .HasColumnName("business_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(x => x.ReportingOutletId)
            .HasColumnName("reporting_outlet_id")
            .IsRequired(false);

        builder.Property(x => x.ReportingOutletCodeSnapshot)
            .HasColumnName("reporting_outlet_code_snapshot")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.ReportingOutletNameSnapshot)
            .HasColumnName("reporting_outlet_name_snapshot")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(x => x.CustomerNameSnapshot)
            .HasColumnName("customer_name_snapshot")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.CustomerEmailSnapshot)
            .HasColumnName("customer_email_snapshot")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.CustomerPhoneSnapshot)
            .HasColumnName("customer_phone_snapshot")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50);

        builder.Property(x => x.TillId)
            .HasColumnName("till_id");

        builder.Property(x => x.TillSessionId)
            .HasColumnName("till_session_id");

        builder.Property(x => x.PriceListId)
            .HasColumnName("price_list_id");

        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.IsTaxInclusive)
            .HasColumnName("is_tax_included")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.SubtotalAmount)
            .HasColumnName("subtotal_amount")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.TaxAmount)
            .HasColumnName("tax_amount")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.ChargeAmount)
            .HasColumnName("charge_amount")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.RoundingAmount)
            .HasColumnName("rounding_amount")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.PaidAmount)
            .HasColumnName("paid_amount")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.RefundedAmount)
            .HasColumnName("refunded_amount")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.BalanceDue)
            .HasColumnName("balance_due")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.Status)
            .HasColumnName("order_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.PaymentStatus)
            .HasColumnName("payment_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.FulfillmentStatus)
            .HasColumnName("fulfillment_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.CustomerNote)
            .HasColumnName("customer_note")
            .HasColumnType("text");

        builder.Property(x => x.InternalNote)
            .HasColumnName("internal_note")
            .HasColumnType("text");

        builder.Property(x => x.PlacedAt)
            .HasColumnName("placed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ConfirmedAt)
            .HasColumnName("confirmed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CancelledAt)
            .HasColumnName("cancelled_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasColumnType("text");

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id");

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_orders_tenant_id_tenants");

        builder.HasOne<DocumentNumberSequence>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.DocumentNumberSequenceId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_orders_document_number_sequence_id_document_number_sequences");

        builder.HasOne<SalesChannel>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesChannelId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_orders_sales_channel_id_sales_channels");

        builder.HasOne<FulfillmentMethodOutlet>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.FulfillmentMethodOutletId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_orders_fulfillment_method_outlet_id_fulfillment_method_outlets");

        builder.HasOne<E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.CustomerId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_orders_customer_id_customers");

        builder.HasOne<Till>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TillId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_orders_till_id_tills");

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReportingOutletId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_orders_reporting_outlet_id_outlets");

        builder.HasOne<TillSession>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TillSessionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_orders_till_session_id_till_sessions");

        builder.HasOne<PriceList>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.PriceListId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_orders_price_list_id_price_lists");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_orders_created_by_tenant_user_id_tenant_users");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_orders_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.OrderNumber })
            .IsUnique()
            .HasDatabaseName("uq_sales_orders_tenant_id_order_number");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_sales_orders_tenant_id_id");

        builder.HasIndex(x => new { x.TenantId, x.BusinessDate, x.Status })
            .HasDatabaseName("ix_sales_orders_tenant_business_date_status");

        builder.HasIndex(x => new { x.TenantId, x.ReportingOutletId, x.BusinessDate })
            .HasDatabaseName("ix_sales_orders_tenant_reporting_outlet_business_date");

        builder.HasIndex(x => new { x.TenantId, x.SalesChannelId, x.BusinessDate })
            .HasDatabaseName("ix_sales_orders_tenant_sales_channel_business_date");

        builder.HasIndex(x => new { x.TenantId, x.PaymentStatus, x.BusinessDate })
            .HasDatabaseName("ix_sales_orders_tenant_payment_status_business_date");

        builder.HasIndex(x => new { x.TenantId, x.CreatedByTenantUserId, x.BusinessDate })
            .HasDatabaseName("ix_sales_orders_tenant_cashier_business_date");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_sales_orders_subtotal_amount", "subtotal_amount >= 0");
            t.HasCheckConstraint("ck_sales_orders_discount_amount", "discount_amount >= 0");
            t.HasCheckConstraint("ck_sales_orders_tax_amount", "tax_amount >= 0");
            t.HasCheckConstraint("ck_sales_orders_charge_amount", "charge_amount >= 0");
            t.HasCheckConstraint("ck_sales_orders_total_amount", "total_amount >= 0");
            t.HasCheckConstraint("ck_sales_orders_paid_amount", "paid_amount >= 0");
            t.HasCheckConstraint("ck_sales_orders_refunded_amount", "refunded_amount >= 0");
            t.HasCheckConstraint("ck_sales_orders_order_type", "order_type IN ('POS_SALE', 'CLICK_AND_COLLECT', 'EXCHANGE_ORDER', 'MANUAL_ORDER')");
            t.HasCheckConstraint("ck_sales_orders_order_status", "order_status IN ('DRAFT', 'PLACED', 'CONFIRMED', 'COMPLETED', 'CANCELLED', 'VOIDED')");
            t.HasCheckConstraint("ck_sales_orders_payment_status", "payment_status IN ('UNPAID', 'PARTIALLY_PAID', 'PAID', 'PARTIALLY_REFUNDED', 'REFUNDED', 'FAILED')");
            t.HasCheckConstraint("ck_sales_orders_fulfillment_status", "fulfillment_status IN ('NOT_REQUIRED', 'PENDING', 'READY_FOR_PICKUP', 'PARTIALLY_FULFILLED', 'FULFILLED', 'CANCELLED')");
        });
    }
}
