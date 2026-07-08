using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.ProductChoiceOptionId)
            .HasColumnName("product_choice_option_id")
            .IsRequired();

        builder.Property(x => x.ImpactProductId)
            .HasColumnName("impact_product_id")
            .IsRequired();

        builder.Property(x => x.ImpactVariantId)
            .HasColumnName("impact_variant_id")
            .IsRequired(false);

        builder.Property(x => x.ImpactUomId)
            .HasColumnName("impact_uom_id")
            .IsRequired();

        builder.Property(x => x.InventoryEffectType)
            .HasColumnName("inventory_effect_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric(18,4)")
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

        builder.HasOne<ProductChoiceOption>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductChoiceOptionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_choice_option_inventory_impacts_product_choice_option_id");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ImpactProductId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_choice_option_inventory_impacts_impact_product_id_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ImpactVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_choice_option_inventory_impacts_impact_variant_id_variants");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.ImpactUomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_choice_option_inventory_impacts_impact_uom_id_uoms");

        builder.HasIndex(x => new { x.ProductChoiceOptionId, x.ImpactProductId, x.ImpactUomId, x.InventoryEffectType })
            .IsUnique()
            .HasDatabaseName("uq_choice_option_inventory_impacts_product_option_product_uom")
            .HasFilter("impact_variant_id IS NULL");

        builder.HasIndex(x => new { x.ProductChoiceOptionId, x.ImpactVariantId, x.ImpactUomId, x.InventoryEffectType })
            .IsUnique()
            .HasDatabaseName("uq_choice_option_inventory_impacts_product_option_variant_uom")
            .HasFilter("impact_variant_id IS NOT NULL");

        builder.ToTable(t => t.HasCheckConstraint("ck_choice_option_inventory_impacts_quantity", "quantity > 0")); 
        builder.ToTable(t => t.HasCheckConstraint("ck_choice_option_inventory_impacts_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')")); 
    }
}


