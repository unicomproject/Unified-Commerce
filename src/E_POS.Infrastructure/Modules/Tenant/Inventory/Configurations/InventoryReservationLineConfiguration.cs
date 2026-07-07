using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Configurations;

public sealed class InventoryReservationLineConfiguration : IEntityTypeConfiguration<InventoryReservationLine>
{
    public void Configure(EntityTypeBuilder<InventoryReservationLine> builder)
    {
        builder.ToTable("inventory_reservation_lines");

        builder.HasKey(x => x.Id).HasName("pk_inventory_reservation_lines");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.InventoryReservationId).HasColumnName("inventory_reservation_id").IsRequired();
        builder.Property(x => x.LineNumber).HasColumnName("line_number").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.ProductVariantId).HasColumnName("product_variant_id").IsRequired(false);
        builder.Property(x => x.RequestedQuantity).HasColumnName("requested_quantity").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ReservedQuantity).HasColumnName("reserved_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.ReleasedQuantity).HasColumnName("released_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.FulfilledQuantity).HasColumnName("fulfilled_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.LineStatus).HasColumnName("line_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reservation_lines_tenant_id_tenants");
        
        builder.HasOne<InventoryReservation>().WithMany().HasForeignKey(x => new { x.TenantId, x.InventoryReservationId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reservation_lines_inventory_reservation_id_inventory_reservations");
        builder.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reservation_lines_product_id_products");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductVariantId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reservation_lines_product_variant_id_product_variants");

        builder.HasIndex(x => new { x.TenantId, x.InventoryReservationId, x.LineNumber }).IsUnique().HasDatabaseName("uq_inventory_reservation_lines_tenant_id_inventory_reservation_id_line_number");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_inventory_reservation_lines_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_inventory_reservation_lines_quantities", "line_number > 0 AND requested_quantity > 0 AND reserved_quantity >= 0 AND released_quantity >= 0 AND fulfilled_quantity >= 0 AND reserved_quantity <= requested_quantity AND released_quantity + fulfilled_quantity <= reserved_quantity");
        });
    }
}


