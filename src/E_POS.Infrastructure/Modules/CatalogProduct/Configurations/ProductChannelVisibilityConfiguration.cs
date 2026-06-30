using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class ProductChannelVisibilityConfiguration : IEntityTypeConfiguration<ProductChannelVisibility>
{
    public void Configure(EntityTypeBuilder<ProductChannelVisibility> builder)
    {
        builder.ToTable("product_channel_visibility");

        builder.HasKey(x => x.Id).HasName("pk_product_channel_visibility");

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

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.SalesChannelId)
            .HasColumnName("sales_channel_id")
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_channel_visibility_product_id_products");
        builder.HasOne<SalesChannel>()
            .WithMany()
            .HasForeignKey(x => x.SalesChannelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_channel_visibility_sales_channel_id_sales_channels");

        builder.HasIndex(x => new { x.ProductId, x.SalesChannelId })
            .IsUnique()
            .HasDatabaseName("uq_product_channel_visibility_product_id_sales_channel_id");
    }
}


