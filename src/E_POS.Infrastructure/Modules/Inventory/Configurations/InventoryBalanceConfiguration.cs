using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Customer.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;
public sealed class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable("inventory_balances");
        builder.HasKey(x => x.Id).HasName("pk_inventory_balances");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.UpdatedBy);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.InventoryLocationId).HasColumnName("inventory_location_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.ProductVariantId).HasColumnName("product_variant_id").IsRequired(false);
        builder.Property(x => x.ProductBatchId).HasColumnName("product_batch_id").IsRequired(false);
        builder.Property(x => x.OnHandQuantity).HasColumnName("on_hand_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.ReservedQuantity).HasColumnName("reserved_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.DamagedQuantity).HasColumnName("damaged_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.QuarantineQuantity).HasColumnName("quarantine_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.AvailableQuantity).HasColumnName("available_quantity").HasPrecision(18, 4).HasComputedColumnSql("on_hand_quantity - reserved_quantity - damaged_quantity - quarantine_quantity", stored: true);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_balances_tenant_id_tenants");
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => x.InventoryLocationId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_balances_inventory_location_id_inventory_locations");
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_balances_product_id_products");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_balances_product_variant_id_product_variants");
        builder.HasOne<ProductBatch>().WithMany().HasForeignKey(x => x.ProductBatchId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_balances_product_batch_id_product_batches");
        builder.HasIndex(x => new { x.TenantId, x.InventoryLocationId, x.ProductId, x.ProductVariantId, x.ProductBatchId }).IsUnique().HasDatabaseName("uq_inventory_balances_scope").AreNullsDistinct(false);
        builder.ToTable(t => { t.HasCheckConstraint("ck_inventory_balances_reserved_quantity", "reserved_quantity >= 0"); t.HasCheckConstraint("ck_inventory_balances_damaged_quantity", "damaged_quantity >= 0"); t.HasCheckConstraint("ck_inventory_balances_quarantine_quantity", "quarantine_quantity >= 0"); });
    }
}