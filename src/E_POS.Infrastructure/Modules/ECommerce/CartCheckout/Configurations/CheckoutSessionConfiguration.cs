using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Configurations;

public sealed class CheckoutSessionConfiguration : IEntityTypeConfiguration<CheckoutSession>
{
    public void Configure(EntityTypeBuilder<CheckoutSession> builder)
    {
        builder.ToTable("checkout_sessions");

        builder.HasKey(x => x.Id).HasName("pk_checkout_sessions");

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

        builder.Property(x => x.SalesChannelId)
            .HasColumnName("sales_channel_id");

        builder.Property(x => x.CartId)
            .HasColumnName("cart_id")
            .IsRequired();

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(x => x.AnonymousSessionId)
            .HasColumnName("anonymous_session_id")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120);

        builder.Property(x => x.CheckoutNumber)
            .HasColumnName("checkout_number")
            .HasColumnType("varchar(60)")
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(x => x.CheckoutStatus)
            .HasColumnName("checkout_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.SalesChannel)
            .HasColumnName("sales_channel")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.FulfillmentMethodCode)
            .HasColumnName("fulfillment_method_code")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.SelectedOutletId)
            .HasColumnName("selected_outlet_id");

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

        builder.Property(x => x.PickupContactName)
            .HasColumnName("pickup_contact_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.PickupContactPhone)
            .HasColumnName("pickup_contact_phone")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50);

        builder.Property(x => x.PickupContactEmail)
            .HasColumnName("pickup_contact_email")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

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
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.TaxAmount)
            .HasColumnName("tax_amount")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.ChargeAmount)
            .HasColumnName("charge_amount")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasColumnName("total_amount")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.InventoryReservationId)
            .HasColumnName("inventory_reservation_id");

        builder.Property(x => x.ConvertedOrderId)
            .HasColumnName("converted_order_id");

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ExpiredAt)
            .HasColumnName("expired_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_sessions_tenant_id_tenants");

        builder.HasOne<E_POS.Domain.Modules.Platform.PlatformFoundation.Entities.PlatformSalesChannel>()
            .WithMany()
            .HasForeignKey(x => x.SalesChannelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_sessions_sales_channel_id_sales_channels");

        builder.HasOne<ShoppingCart>()
            .WithMany()
            .HasForeignKey(x => x.CartId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_sessions_cart_id_shopping_carts");

        builder.HasOne<E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_sessions_customer_id_customers");

        builder.HasOne<E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities.Outlet>()
            .WithMany()
            .HasForeignKey(x => x.SelectedOutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_sessions_selected_outlet_id_outlets");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.ConvertedOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_sessions_converted_order_id_sales_orders");

        builder.HasIndex(x => new { x.TenantId, x.CheckoutNumber })
            .IsUnique()
            .HasDatabaseName("uq_checkout_sessions_tenant_id_checkout_number");

        builder.ToTable(t => {
            t.HasCheckConstraint("ck_checkout_sessions_checkout_status", "checkout_status IN ('STARTED', 'PENDING', 'COMPLETED', 'EXPIRED', 'CANCELLED', 'FAILED')");
            t.HasCheckConstraint("ck_checkout_sessions_subtotal_amount", "subtotal_amount >= 0");
            t.HasCheckConstraint("ck_checkout_sessions_discount_amount", "discount_amount >= 0");
            t.HasCheckConstraint("ck_checkout_sessions_tax_amount", "tax_amount >= 0");
            t.HasCheckConstraint("ck_checkout_sessions_charge_amount", "charge_amount >= 0");
            t.HasCheckConstraint("ck_checkout_sessions_total_amount", "total_amount >= 0");
        });
    }
}
