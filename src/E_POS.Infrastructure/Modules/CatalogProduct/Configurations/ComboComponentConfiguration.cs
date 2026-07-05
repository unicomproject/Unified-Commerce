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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.ComboDefinitionId)
            .HasColumnName("combo_definition_id")
            .IsRequired();

        builder.Property(x => x.ComponentProductId)
            .HasColumnName("component_product_id")
            .IsRequired();

        builder.Property(x => x.ComponentVariantId)
            .HasColumnName("component_variant_id")
            .IsRequired(false);

        builder.Property(x => x.ComponentUomId)
            .HasColumnName("component_uom_id")
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.BasePriceAdjustment)
            .HasColumnName("base_price_adjustment")
            .HasColumnType("numeric(18,4)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        builder.HasOne<ComboDefinition>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ComboDefinitionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_components_combo_definition_id_combo_definitions");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ComponentProductId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_components_component_product_id_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ComponentVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_components_component_variant_id_product_variants");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.ComponentUomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_components_component_uom_id_unit_of_measures");

        builder.HasIndex(x => new { x.ComboDefinitionId, x.ComponentProductId, x.ComponentUomId })
            .IsUnique()
            .HasDatabaseName("uq_combo_components_combo_definition_id_comp_product_uom")
            .HasFilter("component_variant_id IS NULL");

        builder.HasIndex(x => new { x.ComboDefinitionId, x.ComponentVariantId, x.ComponentUomId })
            .IsUnique()
            .HasDatabaseName("uq_combo_components_combo_definition_id_comp_variant_uom")
            .HasFilter("component_variant_id IS NOT NULL");

        builder.ToTable(t => t.HasCheckConstraint("ck_combo_components_quantity", "quantity > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_combo_components_sort_order", "sort_order >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_combo_components_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}
