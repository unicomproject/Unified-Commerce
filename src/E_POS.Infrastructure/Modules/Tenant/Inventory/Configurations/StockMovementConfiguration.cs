using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");

        builder.HasKey(x => x.Id).HasName("pk_stock_movements");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.MovementNumber).HasColumnName("movement_number").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.InventoryBalanceId).HasColumnName("inventory_balance_id").IsRequired();
        builder.Property(x => x.MovementType).HasColumnName("movement_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.QuantityBefore).HasColumnName("quantity_before").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.QuantityChange).HasColumnName("quantity_change").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.QuantityAfter).HasColumnName("quantity_after").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.UnitCost).HasColumnName("unit_cost").HasPrecision(18, 4).IsRequired(false);
        builder.Property(x => x.TotalCost).HasColumnName("total_cost").HasPrecision(18, 4).IsRequired(false);
        builder.Property(x => x.ReasonCode).HasColumnName("reason_code").HasColumnType("varchar(60)").HasMaxLength(60).IsRequired(false);
        builder.Property(x => x.ReferenceNumberSnapshot).HasColumnName("reference_number_snapshot").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired(false);
        builder.Property(x => x.MovementNote).HasColumnName("movement_note").HasColumnType("text").IsRequired(false);
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_movements_tenant_id_tenants");
        
        builder.HasOne<InventoryBalance>().WithMany().HasForeignKey(x => new { x.TenantId, x.InventoryBalanceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_movements_inventory_balance_id_inventory_balances");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_movements_created_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.MovementNumber }).IsUnique().HasDatabaseName("uq_stock_movements_tenant_id_movement_number");
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique().HasDatabaseName("uq_stock_movements_tenant_id_idempotency_key").HasFilter("idempotency_key IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_stock_movements_tenant_id_id");
        builder.HasIndex(x => new { x.TenantId, x.OccurredAt, x.MovementType }).HasDatabaseName("ix_stock_movements_tenant_occurred_at_type");
        builder.HasIndex(x => new { x.TenantId, x.InventoryBalanceId, x.OccurredAt }).HasDatabaseName("ix_stock_movements_tenant_balance_occurred_at");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_stock_movements_quantity_change", "quantity_change <> 0");
            t.HasCheckConstraint("ck_stock_movements_quantity_after", "quantity_after = quantity_before + quantity_change");
            t.HasCheckConstraint("ck_stock_movements_unit_cost", "unit_cost IS NULL OR unit_cost >= 0");
            t.HasCheckConstraint("ck_stock_movements_total_cost", "total_cost IS NULL OR total_cost >= 0");
        });
    }
}


