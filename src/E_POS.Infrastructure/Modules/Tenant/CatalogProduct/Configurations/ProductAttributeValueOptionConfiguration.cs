using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductAttributeValueOptionConfiguration : IEntityTypeConfiguration<ProductAttributeValueOption>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValueOption> builder)
    {
        builder.ToTable("product_attribute_value_options");

        builder.HasKey(x => x.Id).HasName("pk_product_attribute_value_options");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(x => x.UpdatedAt);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.ProductAttributeValueId)
            .HasColumnName("product_attribute_value_id")
            .IsRequired();

        builder.Property(x => x.AttributeDefinitionId)
            .HasColumnName("attribute_definition_id")
            .IsRequired();

        builder.Property(x => x.AttributeOptionId)
            .HasColumnName("attribute_option_id")
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_attribute_value_options_tenant_id_tenants");

        builder.HasOne<ProductAttributeValue>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductAttributeValueId, x.AttributeDefinitionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id, x.AttributeDefinitionId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_attribute_value_options_product_attribute_value_id_product_attribute_values");

        builder.HasOne<ProductAttributeOption>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.AttributeDefinitionId, x.AttributeOptionId })
            .HasPrincipalKey(x => new { x.TenantId, x.AttributeDefinitionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_attribute_value_options_attribute_option_id_product_attribute_options");

        builder.HasIndex(x => new { x.TenantId, x.ProductAttributeValueId, x.AttributeOptionId })
            .IsUnique()
            .HasDatabaseName("uq_product_attribute_val_opts_tenant_id_val_id_opt_id");
    }
}



