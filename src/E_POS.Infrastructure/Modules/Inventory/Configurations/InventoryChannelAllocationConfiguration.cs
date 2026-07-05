using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class InventoryChannelAllocationConfiguration : IEntityTypeConfiguration<InventoryChannelAllocation>
{
    public void Configure(EntityTypeBuilder<InventoryChannelAllocation> builder)
    {
        builder.ToTable("inventory_channel_allocations");

        builder.HasKey(x => x.Id).HasName("pk_inventory_channel_allocations");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.InventoryLocationId).HasColumnName("inventory_location_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.ProductVariantId).HasColumnName("product_variant_id").IsRequired(false);
        builder.Property(x => x.SalesChannelId).HasColumnName("sales_channel_id").IsRequired();
        builder.Property(x => x.AllocationLimitQuantity).HasColumnName("allocation_limit_quantity").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.SafetyStockQuantity).HasColumnName("safety_stock_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_channel_allocations_tenant_id_tenants");
        
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => new { x.TenantId, x.InventoryLocationId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_channel_allocations_inventory_location_id_inventory_locations");
        builder.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_channel_allocations_product_id_products");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductVariantId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_channel_allocations_product_variant_id_product_variants");
        builder.HasOne<SalesChannel>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesChannelId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_channel_allocations_sales_channel_id_sales_channels");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_channel_allocations_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_channel_allocations_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.InventoryLocationId, x.ProductId, x.SalesChannelId }).IsUnique().HasDatabaseName("uq_inventory_channel_allocations_product").HasFilter("product_variant_id IS NULL");
        builder.HasIndex(x => new { x.TenantId, x.InventoryLocationId, x.ProductId, x.ProductVariantId, x.SalesChannelId }).IsUnique().HasDatabaseName("uq_inventory_channel_allocations_variant").HasFilter("product_variant_id IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_inventory_channel_allocations_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_inventory_channel_allocations_allocation_limit_quantity", "allocation_limit_quantity >= 0");
            t.HasCheckConstraint("ck_inventory_channel_allocations_safety_stock_quantity", "safety_stock_quantity >= 0");
            t.HasCheckConstraint("ck_inventory_channel_allocations_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        });
    }
}