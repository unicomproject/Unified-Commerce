using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductAttributeDefinitionConfiguration : IEntityTypeConfiguration<ProductAttributeDefinition>
{
    public void Configure(EntityTypeBuilder<ProductAttributeDefinition> builder)
    {
        builder.ToTable("product_attribute_definitions");

        builder.HasKey(x => x.Id).HasName("pk_product_attribute_definitions");

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

        builder.Property(x => x.AttributeKey)
            .HasColumnName("attribute_key")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.AttributeName)
            .HasColumnName("attribute_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.AttributeType)
            .HasColumnName("attribute_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.DefaultUomId)
            .HasColumnName("default_uom_id")
            .IsRequired(false);

        builder.Property(x => x.IsFilterable)
            .HasColumnName("is_filterable")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsSearchable)
            .HasColumnName("is_searchable")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsRequired)
            .HasColumnName("is_required")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.AppliesTo)
            .HasColumnName("applies_to")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
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

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_attribute_definitions_tenant_id_tenants");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.DefaultUomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_attribute_definitions_default_uom_id_unit_of_measures");

        builder.HasIndex(x => new { x.TenantId, x.AttributeKey })
            .IsUnique()
            .HasDatabaseName("uq_product_attribute_definitions_tenant_id_attribute_key");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_product_attribute_definitions_tenant_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_product_attribute_definitions_sort_order", "sort_order >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_product_attribute_definitions_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}



