using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Configurations;

public sealed class ProductBatchConfiguration : IEntityTypeConfiguration<ProductBatch>
{
    public void Configure(EntityTypeBuilder<ProductBatch> builder)
    {
        builder.ToTable("product_batches");

        builder.HasKey(x => x.Id).HasName("pk_product_batches");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.ProductVariantId).HasColumnName("product_variant_id").IsRequired(false);
        builder.Property(x => x.BatchNumber).HasColumnName("batch_number").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(x => x.SupplierBatchNumber).HasColumnName("supplier_batch_number").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.ManufacturedAt).HasColumnName("manufactured_at").HasColumnType("date").IsRequired(false);
        builder.Property(x => x.ExpiryDate).HasColumnName("expiry_date").HasColumnType("date").IsRequired(false);
        builder.Property(x => x.FirstReceivedAt).HasColumnName("first_received_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_batches_tenant_id_tenants");
        
        builder.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_batches_product_id_products");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductVariantId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_batches_product_variant_id_product_variants");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_batches_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_batches_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.BatchNumber }).IsUnique().HasDatabaseName("uq_product_batches_tenant_id_product_id_batch_number").HasFilter("product_variant_id IS NULL");
        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.ProductVariantId, x.BatchNumber }).IsUnique().HasDatabaseName("uq_product_batches_tenant_id_product_id_product_variant_id_batch_number").HasFilter("product_variant_id IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_product_batches_tenant_id_id");

        builder.ToTable(t => 
        {
            t.HasCheckConstraint("ck_product_batches_expiry_date", "expiry_date IS NULL OR manufactured_at IS NULL OR expiry_date >= manufactured_at");
        });
    }
}


