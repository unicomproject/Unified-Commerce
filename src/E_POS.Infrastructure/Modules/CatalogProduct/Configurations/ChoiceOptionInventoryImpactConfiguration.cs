using E_POS.Domain.Modules.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class ChoiceOptionInventoryImpactConfiguration : IEntityTypeConfiguration<ChoiceOptionInventoryImpact>
{
    public void Configure(EntityTypeBuilder<ChoiceOptionInventoryImpact> builder)
    {
        builder.ToTable("choice_option_inventory_impacts");

        builder.HasKey(x => x.Id).HasName("pk_choice_option_inventory_impacts");

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

        builder.Property(x => x.IngredientProductId)
            .HasColumnName("ingredient_product_id")
            .IsRequired();

        builder.Property(x => x.ProductChoiceOptionId)
            .HasColumnName("product_choice_option_id")
            .IsRequired();

        builder.Property(x => x.QuantityDelta)
            .HasColumnName("quantity_delta")
            .HasPrecision(18, 4);

        builder.HasOne<ProductChoiceOption>()
            .WithMany()
            .HasForeignKey(x => x.ProductChoiceOptionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_choice_option_inventory_impacts_product_choice_option_id_product_choice_options");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.IngredientProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_choice_option_inventory_impacts_ingredient_product_id_products");

        builder.ToTable(t => t.HasCheckConstraint("ck_choice_option_inventory_impacts_quantity_delta", "quantity_delta <> 0")); 
    }
}

