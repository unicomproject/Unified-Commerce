using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(x => x.Id).HasName("pk_products");

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

        builder.Property(x => x.ProductCode)
            .HasColumnName("product_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.ProductName)
            .HasColumnName("product_name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ProductSlug)
            .HasColumnName("product_slug")
            .HasColumnType("varchar(220)")
            .HasMaxLength(220)
            .IsRequired();

        builder.Property(x => x.ProductType)
            .HasColumnName("product_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ProductStructure)
            .HasColumnName("product_structure")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.BusinessTypeId)
            .HasColumnName("business_type_id")
            .IsRequired(false);

        builder.Property(x => x.BrandId)
            .HasColumnName("brand_id")
            .IsRequired(false);

        builder.Property(x => x.ReturnPolicyId)
            .HasColumnName("return_policy_id")
            .IsRequired(false);

        builder.Property(x => x.ShortDescription)
            .HasColumnName("short_description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.LongDescription)
            .HasColumnName("long_description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.IsSellable)
            .HasColumnName("is_sellable")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.IsTaxable)
            .HasColumnName("is_taxable")
            .HasDefaultValue(true)
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

        builder.Property(x => x.CurrentSetupStep)
            .HasColumnName("current_setup_step")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(x => x.DraftSavedAt)
            .HasColumnName("draft_saved_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.PublishedAt)
            .HasColumnName("published_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.PublishedByTenantUserId)
            .HasColumnName("published_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.ArchivedAt)
            .HasColumnName("archived_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ArchivedByTenantUserId)
            .HasColumnName("archived_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasDefaultValue(1)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(x => x.DesiredPublishStatus)
            .HasColumnName("desired_publish_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_products_tenant_id_tenants");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.PublishedByTenantUserId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_products_tenant_users_published_by");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ArchivedByTenantUserId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_products_tenant_users_archived_by");

        builder.HasIndex(x => new { x.TenantId, x.ProductCode })
            .IsUnique()
            .HasDatabaseName("uq_products_tenant_id_product_code");

        builder.HasIndex(x => new { x.TenantId, x.ProductSlug })
            .IsUnique()
            .HasDatabaseName("uq_products_tenant_id_product_slug");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_products_tenant_id_id");

        builder.ToTable(t => 
        {
            t.HasCheckConstraint("ck_products_status", "status IN ('DRAFT', 'ACTIVE', 'INACTIVE', 'ARCHIVED')");
            t.HasCheckConstraint("ck_products_setup_step", "current_setup_step BETWEEN 1 AND 8");
            t.HasCheckConstraint(
                "ck_products_desired_publish_status",
                "desired_publish_status IS NULL OR desired_publish_status IN ('ACTIVE','INACTIVE')");
        });
    }
}
