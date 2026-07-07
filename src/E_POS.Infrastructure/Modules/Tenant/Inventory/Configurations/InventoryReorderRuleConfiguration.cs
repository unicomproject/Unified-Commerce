using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Configurations;

public sealed class InventoryReorderRuleConfiguration : IEntityTypeConfiguration<InventoryReorderRule>
{
    public void Configure(EntityTypeBuilder<InventoryReorderRule> builder)
    {
        builder.ToTable("inventory_reorder_rules");

        builder.HasKey(x => x.Id).HasName("pk_inventory_reorder_rules");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.InventoryLocationId).HasColumnName("inventory_location_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.ProductVariantId).HasColumnName("product_variant_id").IsRequired(false);
        builder.Property(x => x.ReorderMethod).HasColumnName("reorder_method").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ReorderPointQuantity).HasColumnName("reorder_point_quantity").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ReorderQuantity).HasColumnName("reorder_quantity").HasPrecision(18, 4).IsRequired(false);
        builder.Property(x => x.MinStockQuantity).HasColumnName("min_stock_quantity").HasPrecision(18, 4).IsRequired(false);
        builder.Property(x => x.MaxStockQuantity).HasColumnName("max_stock_quantity").HasPrecision(18, 4).IsRequired(false);
        builder.Property(x => x.SafetyStockQuantity).HasColumnName("safety_stock_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.LeadTimeDays).HasColumnName("lead_time_days").IsRequired(false);
        builder.Property(x => x.SupplierProductId).HasColumnName("supplier_product_id").IsRequired(false);
        builder.Property(x => x.IsAutoReorder).HasColumnName("is_auto_reorder").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reorder_rules_tenant_id_tenants");
        
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => new { x.TenantId, x.InventoryLocationId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reorder_rules_inventory_location_id_inventory_locations");
        builder.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reorder_rules_product_id_products");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductVariantId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reorder_rules_product_variant_id_product_variants");

        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reorder_rules_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reorder_rules_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.InventoryLocationId, x.ProductId }).IsUnique().HasDatabaseName("uq_inventory_reorder_rules_product").HasFilter("product_variant_id IS NULL");
        builder.HasIndex(x => new { x.TenantId, x.InventoryLocationId, x.ProductVariantId }).IsUnique().HasDatabaseName("uq_inventory_reorder_rules_variant").HasFilter("product_variant_id IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_inventory_reorder_rules_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_inventory_reorder_rules_quantities", "(reorder_quantity IS NULL OR reorder_quantity > 0) AND reorder_point_quantity >= 0 AND safety_stock_quantity >= 0");
            t.HasCheckConstraint("ck_inventory_reorder_rules_min_max", "max_stock_quantity IS NULL OR min_stock_quantity IS NULL OR max_stock_quantity >= min_stock_quantity");
            t.HasCheckConstraint("ck_inventory_reorder_rules_lead_time_days", "lead_time_days IS NULL OR lead_time_days >= 0");
            t.HasCheckConstraint("ck_inventory_reorder_rules_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        });
    }
}


