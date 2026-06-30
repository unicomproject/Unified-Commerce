using E_POS.Domain.Modules.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class ComboGroupItemConfiguration : IEntityTypeConfiguration<ComboGroupItem>
{
    public void Configure(EntityTypeBuilder<ComboGroupItem> builder)
    {
        builder.ToTable("combo_group_items");

        builder.HasKey(x => x.Id).HasName("pk_combo_group_items");

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

        builder.Property(x => x.ComboGroupId)
            .HasColumnName("combo_group_id")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id");

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order");

        builder.HasOne<ComboGroup>()
            .WithMany()
            .HasForeignKey(x => x.ComboGroupId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_group_items_combo_group_id_combo_groups");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_group_items_product_id_products");

        builder.HasIndex(x => new { x.ComboGroupId, x.ProductId, x.ProductVariantId })
            .IsUnique()
            .HasDatabaseName("uq_combo_group_items_combo_group_id_product_id_product_variant_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_combo_group_items_sort_order", "sort_order >= 0")); 
    }
}

