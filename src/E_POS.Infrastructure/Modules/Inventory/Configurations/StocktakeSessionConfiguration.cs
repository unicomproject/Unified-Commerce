using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class StocktakeSessionConfiguration : IEntityTypeConfiguration<StocktakeSession>
{
    public void Configure(EntityTypeBuilder<StocktakeSession> builder)
    {
        builder.ToTable("stocktake_sessions");

        builder.HasKey(x => x.Id).HasName("pk_stocktake_sessions");

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

        builder.Property(x => x.InventoryLocationId)
            .HasColumnName("inventory_location_id")
            .IsRequired();

        builder.Property(x => x.StocktakeNumber)
            .HasColumnName("stocktake_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.HasOne<InventoryLocation>()
            .WithMany()
            .HasForeignKey(x => x.InventoryLocationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stocktake_sessions_inventory_location_id_inventory_locations");

        builder.HasIndex(x => new { x.TenantId, x.StocktakeNumber })
            .IsUnique()
            .HasDatabaseName("uq_stocktake_sessions_tenant_id_stocktake_number");
    }
}

