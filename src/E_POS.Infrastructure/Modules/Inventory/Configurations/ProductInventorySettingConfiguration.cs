using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class ProductInventorySettingConfiguration : IEntityTypeConfiguration<ProductInventorySetting>
{
    public void Configure(EntityTypeBuilder<ProductInventorySetting> builder)
    {
        builder.ToTable("product_inventory_settings");

        builder.HasKey(x => x.Id).HasName("pk_product_inventory_settings");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.ProductVariantId).HasColumnName("product_variant_id").IsRequired(false);
        builder.Property(x => x.InventoryUomId).HasColumnName("inventory_uom_id").IsRequired();
        builder.Property(x => x.IsStockTracked).HasColumnName("is_stock_tracked").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.AllowNegativeStock).HasColumnName("allow_negative_stock").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.RequiresBatchTracking).HasColumnName("requires_batch_tracking").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.RequiresExpiryTracking).HasColumnName("requires_expiry_tracking").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.RequiresSerialTracking).HasColumnName("requires_serial_tracking").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CostingMethod).HasColumnName("costing_method").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_inventory_settings_tenant_id_tenants");
        
        builder.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_inventory_settings_product_id_products");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductVariantId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_inventory_settings_product_variant_id_product_variants");
        
        builder.HasOne<UnitOfMeasure>().WithMany().HasForeignKey(x => x.InventoryUomId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_inventory_settings_inventory_uom_id_unit_of_measures");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_inventory_settings_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_product_inventory_settings_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.ProductId }).IsUnique().HasDatabaseName("uq_product_inventory_settings_tenant_id_product_id").HasFilter("product_variant_id IS NULL");
        builder.HasIndex(x => new { x.TenantId, x.ProductVariantId }).IsUnique().HasDatabaseName("uq_product_inventory_settings_tenant_id_product_variant_id").HasFilter("product_variant_id IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_product_inventory_settings_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_product_inventory_settings_expiry_requires_batch", "requires_expiry_tracking = false OR requires_batch_tracking = true");
            t.HasCheckConstraint("ck_product_inventory_settings_batch_requires_stock", "requires_batch_tracking = false OR is_stock_tracked = true");
            t.HasCheckConstraint("ck_product_inventory_settings_serial_requires_stock", "requires_serial_tracking = false OR is_stock_tracked = true");
            t.HasCheckConstraint("ck_product_inventory_settings_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        });
    }
}