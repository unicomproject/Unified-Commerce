using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("product_reviews");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(x => x.RatingValue)
            .HasColumnName("rating_value")
            .IsRequired();

        builder.Property(x => x.ReviewTitle)
            .HasColumnName("review_title")
            .HasMaxLength(150);

        builder.Property(x => x.ReviewText)
            .HasColumnName("review_text");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .IsRequired();

        // Auditable Entity mapping
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // Constraints and indexes
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_product_reviews_tenant_id");
        builder.HasIndex(x => x.ProductId).HasDatabaseName("ix_product_reviews_product_id");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("ix_product_reviews_customer_id");
        
        // Tenant FK
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_product_reviews_tenant_id_tenants")
            .OnDelete(DeleteBehavior.Restrict);

        // Product FK
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .HasConstraintName("fk_product_reviews_product_id_products")
            .OnDelete(DeleteBehavior.Cascade);

        // Customer FK
        builder.HasOne<E_POS.Domain.Modules.Customer.Entities.Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .HasConstraintName("fk_product_reviews_customer_id_customers")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
