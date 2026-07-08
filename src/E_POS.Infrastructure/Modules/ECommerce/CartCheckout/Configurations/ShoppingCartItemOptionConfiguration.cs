using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Configurations;

public sealed class ShoppingCartItemOptionConfiguration : IEntityTypeConfiguration<ShoppingCartItemOption>
{
    public void Configure(EntityTypeBuilder<ShoppingCartItemOption> builder)
    {
        builder.ToTable("shopping_cart_item_options");

        builder.HasKey(x => x.Id).HasName("pk_shopping_cart_item_options");

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

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.ShoppingCartItemId)
            .HasColumnName("shopping_cart_item_id")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne<ShoppingCartItem>()
            .WithMany()
            .HasForeignKey(x => x.ShoppingCartItemId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_shopping_cart_item_options_shopping_cart_item_id_shopping_cart_items");

        builder.ToTable(t => t.HasCheckConstraint("ck_shopping_cart_item_options_quantity", "quantity > 0")); 
    }
}



