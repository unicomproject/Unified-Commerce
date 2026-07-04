using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Customer.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;
public sealed class InventoryLocationConfiguration : IEntityTypeConfiguration<InventoryLocation>
{
    public void Configure(EntityTypeBuilder<InventoryLocation> builder)
    {
        builder.ToTable("inventory_locations");
        builder.HasKey(x => x.Id).HasName("pk_inventory_locations");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.ParentInventoryLocationId).HasColumnName("parent_inventory_location_id").IsRequired(false);
        builder.Property(x => x.LocationCode).HasColumnName("location_code").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.LocationName).HasColumnName("location_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.LocationType).HasColumnName("location_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.IsSellableLocation).HasColumnName("is_sellable_location").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.IsReturnLocation).HasColumnName("is_return_location").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.IsReceivingLocation).HasColumnName("is_receiving_location").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.IsQuarantineLocation).HasColumnName("is_quarantine_location").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_locations_tenant_id_tenants");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_locations_outlet_id_outlets");
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => x.ParentInventoryLocationId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_locations_parent_inventory_location_id_inventory_locations");
        builder.HasIndex(x => new { x.TenantId, x.OutletId, x.LocationCode }).IsUnique().HasDatabaseName("uq_inventory_locations_tenant_id_outlet_id_location_code");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_inventory_locations_tenant_id_id");
        builder.ToTable(t => t.HasCheckConstraint("ck_inventory_locations_parent_not_self", "parent_inventory_location_id IS NULL OR parent_inventory_location_id <> id"));
    }
}