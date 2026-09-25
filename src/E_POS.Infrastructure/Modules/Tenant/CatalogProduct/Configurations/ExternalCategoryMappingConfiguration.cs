using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ExternalCategoryMappingConfiguration : IEntityTypeConfiguration<ExternalCategoryMapping>
{
    public void Configure(EntityTypeBuilder<ExternalCategoryMapping> builder)
    {
        builder.ToTable("external_category_mappings");

        builder.HasKey(x => x.Id).HasName("pk_external_category_mappings");

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

        builder.Property(x => x.Provider)
            .HasColumnName("provider")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.ExternalCategoryKey)
            .HasColumnName("external_category_key")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ExternalCategoryName)
            .HasColumnName("external_category_name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.TenantCategoryId)
            .HasColumnName("tenant_category_id")
            .IsRequired();

        builder.Property(x => x.MappingSource)
            .HasColumnName("mapping_source")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .HasDefaultValue("PRODUCT_CONFIRMED")
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
            .HasConstraintName("fk_external_category_mappings_tenant_id_tenants");

        // Database-enforced tenant isolation: Mapping can only reference a Category belonging to the same tenant.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TenantCategoryId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_external_category_mappings_tenant_category");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_external_category_mappings_created_by_tenant_user_id");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_external_category_mappings_updated_by_tenant_user_id");

        builder.HasIndex(x => new { x.TenantId, x.Provider, x.ExternalCategoryKey })
            .IsUnique()
            .HasDatabaseName("uq_external_category_mappings_tenant_provider_key");

        builder.HasIndex(x => new { x.TenantId, x.TenantCategoryId })
            .HasDatabaseName("ix_external_category_mappings_tenant_tenant_category_id");
    }
}
