using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Discount.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Discount.Configurations;

public sealed class DiscountPolicyTargetConfiguration : IEntityTypeConfiguration<DiscountPolicyTarget>
{
    public void Configure(EntityTypeBuilder<DiscountPolicyTarget> builder)
    {
        builder.ToTable("discount_policy_targets");

        builder.HasKey(x => x.Id).HasName("pk_discount_policy_targets");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.DiscountPolicyId).HasColumnName("discount_policy_id").IsRequired();
        builder.Property(x => x.TargetType).HasColumnName("target_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.TargetMode).HasColumnName("target_mode").HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired(false);
        builder.Property(x => x.ProductVariantId).HasColumnName("product_variant_id").IsRequired(false);
        builder.Property(x => x.CategoryId).HasColumnName("category_id").IsRequired(false);
        builder.Property(x => x.BrandId).HasColumnName("brand_id").IsRequired(false);
        builder.Property(x => x.CollectionId).HasColumnName("collection_id").IsRequired(false);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_targets_tenant_id_tenants");
        
        builder.HasOne<DiscountPolicy>().WithMany().HasForeignKey(x => new { x.TenantId, x.DiscountPolicyId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_targets_discount_policy_id_discount_policies");
        builder.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_targets_product_id_products");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductVariantId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_targets_product_variant_id_product_variants");
        builder.HasOne<Category>().WithMany().HasForeignKey(x => new { x.TenantId, x.CategoryId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_targets_category_id_categories");
        builder.HasOne<Brand>().WithMany().HasForeignKey(x => new { x.TenantId, x.BrandId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_targets_brand_id_brands");
        builder.HasOne<Collection>().WithMany().HasForeignKey(x => new { x.TenantId, x.CollectionId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_targets_collection_id_collections");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_targets_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_targets_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_discount_policy_targets_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_discount_policy_targets_target_mode", "target_mode IN ('INCLUDE', 'EXCLUDE')");
            t.HasCheckConstraint("ck_discount_policy_targets_target_type", "target_type IN ('PRODUCT', 'PRODUCT_VARIANT', 'CATEGORY', 'BRAND', 'COLLECTION')");
            t.HasCheckConstraint("ck_discount_policy_targets_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
            t.HasCheckConstraint("ck_discount_policy_targets_one_target", "((target_type = 'PRODUCT' AND product_id IS NOT NULL AND product_variant_id IS NULL AND category_id IS NULL AND brand_id IS NULL AND collection_id IS NULL) OR (target_type = 'PRODUCT_VARIANT' AND product_id IS NULL AND product_variant_id IS NOT NULL AND category_id IS NULL AND brand_id IS NULL AND collection_id IS NULL) OR (target_type = 'CATEGORY' AND product_id IS NULL AND product_variant_id IS NULL AND category_id IS NOT NULL AND brand_id IS NULL AND collection_id IS NULL) OR (target_type = 'BRAND' AND product_id IS NULL AND product_variant_id IS NULL AND category_id IS NULL AND brand_id IS NOT NULL AND collection_id IS NULL) OR (target_type = 'COLLECTION' AND product_id IS NULL AND product_variant_id IS NULL AND category_id IS NULL AND brand_id IS NULL AND collection_id IS NOT NULL))");
        });
    }
}