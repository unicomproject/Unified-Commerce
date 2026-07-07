using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Configurations;

public sealed class StocktakeSessionConfiguration : IEntityTypeConfiguration<StocktakeSession>
{
    public void Configure(EntityTypeBuilder<StocktakeSession> builder)
    {
        builder.ToTable("stocktake_sessions");

        builder.HasKey(x => x.Id).HasName("pk_stocktake_sessions");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id").IsRequired(false);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.StocktakeNumber).HasColumnName("stocktake_number").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.InventoryLocationId).HasColumnName("inventory_location_id").IsRequired();
        builder.Property(x => x.StocktakeType).HasColumnName("stocktake_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.StocktakeStatus).HasColumnName("stocktake_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.IsBlindCount).HasColumnName("is_blind_count").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.SnapshotAt).HasColumnName("snapshot_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.PostedAt).HasColumnName("posted_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.StartedByTenantUserId).HasColumnName("started_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.CompletedByTenantUserId).HasColumnName("completed_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.PostedByTenantUserId).HasColumnName("posted_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.GeneratedStockAdjustmentId).HasColumnName("generated_stock_adjustment_id").IsRequired(false);
        builder.Property(x => x.Notes).HasColumnName("notes").HasColumnType("text").IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_sessions_tenant_id_tenants");
        
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => new { x.TenantId, x.InventoryLocationId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_sessions_inventory_location_id_inventory_locations");
        builder.HasOne<StockAdjustment>().WithMany().HasForeignKey(x => new { x.TenantId, x.GeneratedStockAdjustmentId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_sessions_generated_stock_adjustment_id_stock_adjustments");

        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_sessions_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_sessions_updated_by_tenant_user_id_tenant_users");

        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.StartedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_sessions_started_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CompletedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_sessions_completed_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.PostedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_sessions_posted_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.StocktakeNumber }).IsUnique().HasDatabaseName("uq_stocktake_sessions_tenant_id_stocktake_number");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_stocktake_sessions_tenant_id_id");
    }
}


