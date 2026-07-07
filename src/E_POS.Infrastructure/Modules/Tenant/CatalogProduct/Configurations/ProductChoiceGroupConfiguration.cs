using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductChoiceGroupConfiguration : IEntityTypeConfiguration<ProductChoiceGroup>
{
    public void Configure(EntityTypeBuilder<ProductChoiceGroup> builder)
    {
        builder.ToTable("product_choice_groups");

        builder.HasKey(x => x.Id).HasName("pk_product_choice_groups");

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

        builder.Property(x => x.ChoiceGroupId)
            .HasColumnName("choice_group_id")
            .IsRequired();

        builder.Property(x => x.MinSelectOverride)
            .HasColumnName("min_select_override")
            .IsRequired(false);

        builder.Property(x => x.MaxSelectOverride)
            .HasColumnName("max_select_override")
            .IsRequired(false);

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

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_choice_groups_product_id_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_choice_groups_product_variant_id_product_variants");

        builder.HasOne<ChoiceGroup>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ChoiceGroupId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_choice_groups_choice_group_id_choice_groups");

        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.ChoiceGroupId })
            .IsUnique()
            .HasDatabaseName("uq_product_choice_groups_tenant_id_product_id_choice_group_id")
            .HasFilter("product_variant_id IS NULL");

        builder.HasIndex(x => new { x.TenantId, x.ProductVariantId, x.ChoiceGroupId })
            .IsUnique()
            .HasDatabaseName("uq_product_choice_groups_tenant_id_variant_id_choice_group_id")
            .HasFilter("product_variant_id IS NOT NULL");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_product_choice_groups_tenant_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_product_choice_groups_min_select", "min_select_override IS NULL OR min_select_override >= 0")); 
        builder.ToTable(t => t.HasCheckConstraint("ck_product_choice_groups_max_select", "max_select_override IS NULL OR max_select_override > 0")); 
        builder.ToTable(t => t.HasCheckConstraint("ck_product_choice_groups_max_select_min_select", "max_select_override IS NULL OR min_select_override IS NULL OR max_select_override >= min_select_override")); 
        builder.ToTable(t => t.HasCheckConstraint("ck_product_choice_groups_sort_order", "sort_order >= 0")); 
        builder.ToTable(t => t.HasCheckConstraint("ck_product_choice_groups_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')")); 
    }
}


