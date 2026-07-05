using E_POS.Domain.Modules.Inventory.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class StockMovementSerialConfiguration : IEntityTypeConfiguration<StockMovementSerial>
{
    public void Configure(EntityTypeBuilder<StockMovementSerial> builder)
    {
        builder.ToTable("stock_movement_serials");

        builder.HasKey(x => x.Id).HasName("pk_stock_movement_serials");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.StockMovementId).HasColumnName("stock_movement_id").IsRequired();
        builder.Property(x => x.SerialNumberId).HasColumnName("serial_number_id").IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_movement_serials_tenant_id_tenants");
        
        builder.HasOne<StockMovement>().WithMany().HasForeignKey(x => new { x.TenantId, x.StockMovementId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_movement_serials_stock_movement_id_stock_movements");
        builder.HasOne<SerialNumber>().WithMany().HasForeignKey(x => new { x.TenantId, x.SerialNumberId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_movement_serials_serial_number_id_serial_numbers");

        builder.HasIndex(x => new { x.TenantId, x.StockMovementId, x.SerialNumberId }).IsUnique().HasDatabaseName("uq_stock_movement_serials_tenant_id_stock_movement_id_serial_number_id");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_stock_movement_serials_tenant_id_id");
    }
}