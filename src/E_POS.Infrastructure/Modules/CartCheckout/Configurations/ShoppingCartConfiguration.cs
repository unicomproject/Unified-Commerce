using E_POS.Domain.Modules.CartCheckout.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CartCheckout.Configurations;

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

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_shopping_carts_tenant_id_tenants");

        builder.HasIndex(x => new { x.TenantId, x.CartNumber })
            .IsUnique()
            .HasDatabaseName("uq_shopping_carts_tenant_id_cart_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_shopping_carts_cart_status", "cart_status IN ('ACTIVE', 'CHECKED_OUT', 'ABANDONED', 'EXPIRED', 'CANCELLED')")); 
    }
}

