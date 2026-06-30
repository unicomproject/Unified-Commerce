using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("stock_transfers");

        builder.HasKey(x => x.Id).HasName("pk_stock_transfers");

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
            .HasColumnName("tenant_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.DestinationInventoryLocationId)
            .HasColumnName("destination_inventory_location_id")
            .IsRequired();

        builder.Property(x => x.SourceInventoryLocationId)
            .HasColumnName("source_inventory_location_id")
            .IsRequired();

        builder.Property(x => x.TransferNumber)
            .HasColumnName("transfer_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.HasOne<InventoryLocation>()
            .WithMany()
            .HasForeignKey(x => x.SourceInventoryLocationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stock_transfers_source_inventory_location_id_inventory_locations");

        builder.HasOne<InventoryLocation>()
            .WithMany()
            .HasForeignKey(x => x.DestinationInventoryLocationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stock_transfers_destination_inventory_location_id_inventory_locations");

        builder.HasIndex(x => new { x.TenantId, x.TransferNumber })
            .IsUnique()
            .HasDatabaseName("uq_stock_transfers_tenant_id_transfer_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_stock_transfers_source_inventory_location_id_destination_inventory_location_id", "source_inventory_location_id <> destination_inventory_location_id")); 
    }
}

