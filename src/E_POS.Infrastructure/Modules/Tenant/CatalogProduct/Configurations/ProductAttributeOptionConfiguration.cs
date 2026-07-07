using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductAttributeOptionConfiguration : IEntityTypeConfiguration<ProductAttributeOption>
{
    public void Configure(EntityTypeBuilder<ProductAttributeOption> builder)
    {
        builder.ToTable("product_attribute_options");

        builder.HasKey(x => x.Id).HasName("pk_product_attribute_options");

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

        builder.Property(x => x.AttributeDefinitionId)
            .HasColumnName("attribute_definition_id")
            .IsRequired();

        builder.Property(x => x.OptionCode)
            .HasColumnName("option_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.OptionLabel)
            .HasColumnName("option_label")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
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
            .HasConstraintName("fk_product_attribute_options_tenant_id_tenants");

        builder.HasOne<ProductAttributeDefinition>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.AttributeDefinitionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_attribute_options_attribute_definition_id_product_attribute_definitions");

        builder.HasIndex(x => new { x.TenantId, x.AttributeDefinitionId, x.OptionCode })
            .IsUnique()
            .HasDatabaseName("uq_product_attribute_options_tenant_id_attribute_def_id_opt_code");

        builder.HasIndex(x => new { x.TenantId, x.AttributeDefinitionId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_product_attribute_options_tenant_id_attribute_def_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_product_attribute_options_sort_order", "sort_order >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_product_attribute_options_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}



