using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Configurations;

public sealed class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        builder.ToTable("shopping_carts");

        builder.HasKey(x => x.Id).HasName("pk_shopping_carts");

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

        builder.Property(x => x.CartStatus)
            .HasColumnName("cart_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.CartNumber)
            .HasColumnName("cart_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.SalesChannelId)
            .HasColumnName("sales_channel_id");

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(x => x.AnonymousSessionId)
            .HasColumnName("anonymous_session_id")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120);

        builder.Property(x => x.SalesChannel)
            .HasColumnName("sales_channel")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();

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

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ConvertedCheckoutSessionId)
            .HasColumnName("converted_checkout_session_id");

        builder.Property(x => x.ConvertedOrderId)
            .HasColumnName("converted_order_id");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_shopping_carts_tenant_id_tenants");

        builder.HasOne<E_POS.Domain.Modules.Platform.PlatformFoundation.Entities.PlatformSalesChannel>()
            .WithMany()
            .HasForeignKey(x => x.SalesChannelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_shopping_carts_sales_channel_id_sales_channels");

        builder.HasOne<E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_shopping_carts_customer_id_customers");

        builder.HasIndex(x => new { x.TenantId, x.CartNumber })
            .IsUnique()
            .HasDatabaseName("uq_shopping_carts_tenant_id_cart_number");

        builder.ToTable(t => {
            t.HasCheckConstraint("ck_shopping_carts_cart_status", "cart_status IN ('ACTIVE', 'ABANDONED', 'CONVERTED', 'EXPIRED', 'CANCELLED')");
            t.HasCheckConstraint("ck_shopping_carts_subtotal_amount", "subtotal_amount >= 0");
            t.HasCheckConstraint("ck_shopping_carts_discount_amount", "discount_amount >= 0");
            t.HasCheckConstraint("ck_shopping_carts_tax_amount", "tax_amount >= 0");
            t.HasCheckConstraint("ck_shopping_carts_charge_amount", "charge_amount >= 0");
            t.HasCheckConstraint("ck_shopping_carts_total_amount", "total_amount >= 0");
        });
    }
}




