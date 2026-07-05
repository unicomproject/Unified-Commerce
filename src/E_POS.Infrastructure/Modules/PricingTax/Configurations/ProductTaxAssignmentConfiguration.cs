using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.PricingTax.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.PricingTax.Configurations;

public sealed class ProductTaxAssignmentConfiguration : IEntityTypeConfiguration<ProductTaxAssignment>
{
    public void Configure(EntityTypeBuilder<ProductTaxAssignment> builder)
    {
        builder.ToTable("product_tax_assignments");

        builder.HasKey(x => x.Id).HasName("pk_product_tax_assignments");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.ProductVariantId).HasColumnName("product_variant_id").IsRequired(false);
        builder.Property(x => x.TaxClassId).HasColumnName("tax_class_id").IsRequired();
        builder.Property(x => x.AppliesFrom).HasColumnName("applies_from").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.AppliesUntil).HasColumnName("applies_until").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_tax_assignments_tenant_id_tenants");
        
        builder.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_tax_assignments_product_id_products");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductVariantId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_tax_assignments_product_variant_id_product_variants");
        builder.HasOne<TaxClass>().WithMany().HasForeignKey(x => new { x.TenantId, x.TaxClassId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_tax_assignments_tax_class_id_tax_classes");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_tax_assignments_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_tax_assignments_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.ProductId })
            .IsUnique()
            .HasDatabaseName("uq_product_tax_assignments_active_product")
            .HasFilter("product_variant_id IS NULL AND status = 'ACTIVE'");

        builder.HasIndex(x => new { x.TenantId, x.ProductVariantId })
            .IsUnique()
            .HasDatabaseName("uq_product_tax_assignments_active_variant")
            .HasFilter("product_variant_id IS NOT NULL AND status = 'ACTIVE'");

        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_product_tax_assignments_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_product_tax_assignments_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
            t.HasCheckConstraint("ck_product_tax_assignments_applies_period", "applies_until IS NULL OR applies_from IS NULL OR applies_until >= applies_from");
        });
    }
}
