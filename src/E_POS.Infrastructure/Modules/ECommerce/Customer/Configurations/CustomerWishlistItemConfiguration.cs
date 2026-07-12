using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.Customer.Configurations;

public class CustomerWishlistItemConfiguration : IEntityTypeConfiguration<CustomerWishlistItem>
{
    public void Configure(EntityTypeBuilder<CustomerWishlistItem> builder)
    {
        builder.ToTable("customer_wishlist_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.WishlistId)
            .HasColumnName("wishlist_id")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id");

        builder.Property(x => x.AddedAt)
            .HasColumnName("added_at")
            .IsRequired();

        // Auditable Entity mapping
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // Constraints and indexes
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_customer_wishlist_items_tenant_id");
        builder.HasIndex(x => x.WishlistId).HasDatabaseName("ix_customer_wishlist_items_wishlist_id");
        builder.HasIndex(x => x.ProductId).HasDatabaseName("ix_customer_wishlist_items_product_id");

        builder.HasIndex(x => new { x.WishlistId, x.ProductId, x.ProductVariantId })
            .IsUnique()
            .HasDatabaseName("ix_customer_wishlist_items_unique_product");
        
        // Tenant FK
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_customer_wishlist_items_tenant_id_tenants")
            .OnDelete(DeleteBehavior.Restrict);

        // Product FK
        builder.HasOne<E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .HasConstraintName("fk_customer_wishlist_items_product_id_products")
            .OnDelete(DeleteBehavior.Cascade);
            
        // Product Variant FK
        builder.HasOne<E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .HasConstraintName("fk_customer_wishlist_items_product_variant_id_product_variants")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
