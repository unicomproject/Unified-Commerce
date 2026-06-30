using E_POS.Domain.Modules.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class ComboComponentConfiguration : IEntityTypeConfiguration<ComboComponent>
{
    public void Configure(EntityTypeBuilder<ComboComponent> builder)
    {
        builder.ToTable("combo_components");

        builder.HasKey(x => x.Id).HasName("pk_combo_components");

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

        builder.Property(x => x.ComboDefinitionId)
            .HasColumnName("combo_definition_id")
            .IsRequired();

        builder.Property(x => x.ComponentProductId)
            .HasColumnName("component_product_id")
            .IsRequired();

        builder.Property(x => x.ComponentVariantId)
            .HasColumnName("component_variant_id");

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne<ComboDefinition>()
            .WithMany()
            .HasForeignKey(x => x.ComboDefinitionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_components_combo_definition_id_combo_definitions");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_components_component_product_id_products");

        builder.HasIndex(x => new { x.ComboDefinitionId, x.ComponentProductId, x.ComponentVariantId })
            .IsUnique()
            .HasDatabaseName("uq_combo_components_combo_definition_id_component_product_id_component_variant_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_combo_components_quantity", "quantity > 0")); 
    }
}

