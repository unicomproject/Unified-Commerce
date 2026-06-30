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

        builder.Property(x => x.AllocationLimitQuantity)
            .HasColumnName("allocation_limit_quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.InventoryLocationId)
            .HasColumnName("inventory_location_id")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id");

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id");

        builder.Property(x => x.SalesChannelId)
            .HasColumnName("sales_channel_id")
            .IsRequired();

        builder.HasOne<InventoryLocation>()
            .WithMany()
            .HasForeignKey(x => x.InventoryLocationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventory_channel_allocations_inventory_location_id_inventory_locations");
        builder.HasOne<SalesChannel>()
            .WithMany()
            .HasForeignKey(x => x.SalesChannelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventory_channel_allocations_sales_channel_id_sales_channels");

        builder.HasIndex(x => new { x.InventoryLocationId, x.ProductId, x.ProductVariantId, x.SalesChannelId })
            .IsUnique()
            .HasDatabaseName("uq_inventory_channel_allocations_inventory_location_id_product_id_product_variant_id_sales_channel_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_inventory_channel_allocations_allocation_limit_quantity", "allocation_limit_quantity >= 0")); 
    }
}


