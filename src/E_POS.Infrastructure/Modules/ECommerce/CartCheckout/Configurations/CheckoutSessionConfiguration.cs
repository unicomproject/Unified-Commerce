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
            .HasColumnName("tenant_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CheckoutNumber)
            .HasColumnName("checkout_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.ShoppingCartId)
            .HasColumnName("shopping_cart_id")
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 2);

        builder.HasOne<ShoppingCart>()
            .WithMany()
            .HasForeignKey(x => x.ShoppingCartId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_sessions_shopping_cart_id_shopping_carts");

        builder.HasIndex(x => new { x.TenantId, x.CheckoutNumber })
            .IsUnique()
            .HasDatabaseName("uq_checkout_sessions_tenant_id_checkout_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_checkout_sessions_total_amount", "total_amount >= 0")); 
    }
}



