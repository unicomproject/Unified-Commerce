using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.ToTable("product_attribute_values");

        builder.HasKey(x => x.Id).HasName("pk_product_attribute_values");

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

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id")
            .IsRequired(false);

        builder.Property(x => x.AttributeDefinitionId)
            .HasColumnName("attribute_definition_id")
            .IsRequired();

        builder.Property(x => x.AttributeValueText)
            .HasColumnName("attribute_value_text")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.AttributeValueNumber)
            .HasColumnName("attribute_value_number")
            .HasColumnType("numeric(18,6)")
            .IsRequired(false);

        builder.Property(x => x.AttributeValueBoolean)
            .HasColumnName("attribute_value_boolean")
            .IsRequired(false);

        builder.Property(x => x.AttributeValueDate)
            .HasColumnName("attribute_value_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(x => x.AttributeValueUomId)
            .HasColumnName("attribute_value_uom_id")
            .IsRequired(false);

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

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_attribute_values_tenant_id_tenants");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_attribute_values_product_id_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_attribute_values_product_variant_id_product_variants");

        builder.HasOne<ProductAttributeDefinition>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.AttributeDefinitionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_attribute_values_attribute_definition_id_product_attribute_definitions");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.AttributeValueUomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_attribute_values_attribute_value_uom_id_unit_of_measures");

        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.AttributeDefinitionId })
            .IsUnique()
            .HasDatabaseName("uq_product_attribute_values_tenant_id_product_id_attr_def_id")
            .HasFilter("product_variant_id IS NULL");

        builder.HasIndex(x => new { x.TenantId, x.ProductVariantId, x.AttributeDefinitionId })
            .IsUnique()
            .HasDatabaseName("uq_product_attribute_values_tenant_id_variant_id_attr_def_id")
            .HasFilter("product_variant_id IS NOT NULL");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_product_attribute_values_tenant_id_id");

        builder.HasIndex(x => new { x.TenantId, x.Id, x.AttributeDefinitionId })
            .IsUnique()
            .HasDatabaseName("uq_product_attribute_values_tenant_id_id_attr_def_id");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_product_attribute_values_nonnull_values",
            "(CASE WHEN attribute_value_text IS NOT NULL THEN 1 ELSE 0 END + " +
            "CASE WHEN attribute_value_number IS NOT NULL THEN 1 ELSE 0 END + " +
            "CASE WHEN attribute_value_boolean IS NOT NULL THEN 1 ELSE 0 END + " +
            "CASE WHEN attribute_value_date IS NOT NULL THEN 1 ELSE 0 END) <= 1"
        ));

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_product_attribute_values_uom_requires_number",
            "attribute_value_uom_id IS NULL OR attribute_value_number IS NOT NULL"
        ));

        builder.ToTable(t => t.HasCheckConstraint("ck_product_attribute_values_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}



