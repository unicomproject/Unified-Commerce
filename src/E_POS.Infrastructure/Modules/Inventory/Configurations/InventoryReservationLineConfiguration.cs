using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class InventoryReservationLineConfiguration : IEntityTypeConfiguration<InventoryReservationLine>
{
    public void Configure(EntityTypeBuilder<InventoryReservationLine> builder)
    {
        builder.ToTable("inventory_reservation_lines");

        builder.HasKey(x => x.Id).HasName("pk_inventory_reservation_lines");

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

        builder.Property(x => x.InventoryReservationId)
            .HasColumnName("inventory_reservation_id")
            .IsRequired();

        builder.Property(x => x.LineNumber)
            .HasColumnName("line_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.RequestedQuantity)
            .HasColumnName("requested_quantity")
            .HasPrecision(18, 4);

        builder.HasOne<InventoryReservation>()
            .WithMany()
            .HasForeignKey(x => x.InventoryReservationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventory_reservation_lines_inventory_reservation_id_inventory_reservations");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventory_reservation_lines_product_id_products");

        builder.HasIndex(x => new { x.InventoryReservationId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("uq_inventory_reservation_lines_inventory_reservation_id_line_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_inventory_reservation_lines_requested_quantity", "requested_quantity > 0")); 
    }
}

