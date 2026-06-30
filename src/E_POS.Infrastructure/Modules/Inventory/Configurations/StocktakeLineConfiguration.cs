using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class StocktakeLineConfiguration : IEntityTypeConfiguration<StocktakeLine>
{
    public void Configure(EntityTypeBuilder<StocktakeLine> builder)
    {
        builder.ToTable("stocktake_lines");

        builder.HasKey(x => x.Id).HasName("pk_stocktake_lines");

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

        builder.Property(x => x.ProductBatchId)
            .HasColumnName("product_batch_id");

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id");

        builder.Property(x => x.StocktakeSessionId)
            .HasColumnName("stocktake_session_id")
            .IsRequired();

        builder.HasOne<StocktakeSession>()
            .WithMany()
            .HasForeignKey(x => x.StocktakeSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stocktake_lines_stocktake_session_id_stocktake_sessions");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stocktake_lines_product_id_products");

        builder.HasIndex(x => new { x.StocktakeSessionId, x.ProductId, x.ProductVariantId, x.ProductBatchId })
            .IsUnique()
            .HasDatabaseName("uq_stocktake_lines_stocktake_session_id_product_id_product_variant_id_product_batch_id");
    }
}

