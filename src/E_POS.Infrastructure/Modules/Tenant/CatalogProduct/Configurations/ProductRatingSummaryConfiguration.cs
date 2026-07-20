using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public class ProductRatingSummaryConfiguration : IEntityTypeConfiguration<ProductRatingSummary>
{
    public void Configure(EntityTypeBuilder<ProductRatingSummary> builder)
    {
        builder.ToTable("product_rating_summaries", table =>
        {
            table.HasCheckConstraint(
                "ck_product_rating_summaries_average",
                "average_rating BETWEEN 0 AND 5");
            table.HasCheckConstraint(
                "ck_product_rating_summaries_counts",
                "total_reviews >= 0 AND five_star_count >= 0 AND four_star_count >= 0 AND three_star_count >= 0 AND two_star_count >= 0 AND one_star_count >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.AverageRating)
            .HasColumnName("average_rating")
            .HasColumnType("numeric(3,2)")
            .IsRequired();

        builder.Property(x => x.TotalReviews)
            .HasColumnName("total_reviews")
            .IsRequired();

        builder.Property(x => x.FiveStarCount).HasColumnName("five_star_count").IsRequired();
        builder.Property(x => x.FourStarCount).HasColumnName("four_star_count").IsRequired();
        builder.Property(x => x.ThreeStarCount).HasColumnName("three_star_count").IsRequired();
        builder.Property(x => x.TwoStarCount).HasColumnName("two_star_count").IsRequired();
        builder.Property(x => x.OneStarCount).HasColumnName("one_star_count").IsRequired();

        // Auditable Entity mapping
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // Constraints and indexes
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_product_rating_summaries_tenant_id");

        builder.HasIndex(x => new { x.TenantId, x.ProductId })
            .IsUnique()
            .HasDatabaseName("ux_product_rating_summaries_tenant_product");

        // Tenant FK
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_product_rating_summaries_tenant_id_tenants")
            .OnDelete(DeleteBehavior.Restrict);

        // Product FK
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .HasConstraintName("fk_product_rating_summaries_product_id_products")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
