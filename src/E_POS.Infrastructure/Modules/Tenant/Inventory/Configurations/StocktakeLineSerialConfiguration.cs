using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Configurations;

public sealed class StocktakeLineSerialConfiguration : IEntityTypeConfiguration<StocktakeLineSerial>
{
    public void Configure(EntityTypeBuilder<StocktakeLineSerial> builder)
    {
        builder.ToTable("stocktake_line_serials");

        builder.HasKey(x => x.Id).HasName("pk_stocktake_line_serials");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.StocktakeLineId).HasColumnName("stocktake_line_id").IsRequired();
        builder.Property(x => x.SerialNumberId).HasColumnName("serial_number_id").IsRequired(false);
        builder.Property(x => x.ScannedSerialNumber).HasColumnName("scanned_serial_number").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.CountResult).HasColumnName("count_result").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ScannedByTenantUserId).HasColumnName("scanned_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.ScannedAt).HasColumnName("scanned_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_line_serials_tenant_id_tenants");
        
        builder.HasOne<StocktakeLine>().WithMany().HasForeignKey(x => new { x.TenantId, x.StocktakeLineId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_line_serials_stocktake_line_id_stocktake_lines");
        builder.HasOne<SerialNumber>().WithMany().HasForeignKey(x => new { x.TenantId, x.SerialNumberId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_line_serials_serial_number_id_serial_numbers");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.ScannedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_line_serials_scanned_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.StocktakeLineId, x.ScannedSerialNumber }).IsUnique().HasDatabaseName("uq_stocktake_line_serials_tenant_id_stocktake_line_id_scanned_serial_number");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_stocktake_line_serials_tenant_id_id");
    }
}


