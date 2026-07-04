using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;
public sealed class StockMovementReferenceConfiguration : IEntityTypeConfiguration<StockMovementReference>
{
    public void Configure(EntityTypeBuilder<StockMovementReference> builder)
    { builder.ToTable("stock_movement_references"); builder.HasKey(x => x.Id).HasName("pk_stock_movement_references"); builder.Property(x => x.Id).HasColumnName("id"); builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired(); builder.Ignore(x => x.CreatedBy); builder.Ignore(x => x.UpdatedAt); builder.Ignore(x => x.UpdatedBy); builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired(); builder.Property(x => x.StockMovementId).HasColumnName("stock_movement_id").IsRequired(); builder.Property(x => x.ReferenceType).HasColumnName("reference_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired(); builder.Property(x => x.ReferenceId).HasColumnName("reference_id").IsRequired(); builder.Property(x => x.ReferenceLineId).HasColumnName("reference_line_id").IsRequired(false); builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_movement_references_tenant_id_tenants"); builder.HasOne<StockMovement>().WithMany().HasForeignKey(x => x.StockMovementId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_movement_references_stock_movement_id_stock_movements"); builder.HasIndex(x => new { x.TenantId, x.StockMovementId, x.ReferenceType, x.ReferenceId, x.ReferenceLineId }).IsUnique().HasDatabaseName("uq_stock_movement_references_scope").AreNullsDistinct(false); }
}