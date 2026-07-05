using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("stock_transfers");

        builder.HasKey(x => x.Id).HasName("pk_stock_transfers");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.TransferNumber).HasColumnName("transfer_number").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.SourceLocationId).HasColumnName("source_location_id").IsRequired();
        builder.Property(x => x.DestinationLocationId).HasColumnName("destination_location_id").IsRequired();
        builder.Property(x => x.TransferStatus).HasColumnName("transfer_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.RequestedAt).HasColumnName("requested_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ApprovedAt).HasColumnName("approved_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.ShippedAt).HasColumnName("shipped_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.ReceivedAt).HasColumnName("received_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.ApprovedByTenantUserId).HasColumnName("approved_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.ShippedByTenantUserId).HasColumnName("shipped_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.ReceivedByTenantUserId).HasColumnName("received_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.CancelledByTenantUserId).HasColumnName("cancelled_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.TransferNote).HasColumnName("transfer_note").HasColumnType("text").IsRequired(false);
        builder.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasColumnType("text").IsRequired(false);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfers_tenant_id_tenants");
        
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => new { x.TenantId, x.SourceLocationId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfers_source_location_id_inventory_locations");
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => new { x.TenantId, x.DestinationLocationId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfers_destination_location_id_inventory_locations");

        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfers_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfers_updated_by_tenant_user_id_tenant_users");

        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.ApprovedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfers_approved_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.ShippedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfers_shipped_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.ReceivedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfers_received_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CancelledByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfers_cancelled_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.TransferNumber }).IsUnique().HasDatabaseName("uq_stock_transfers_tenant_id_transfer_number");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_stock_transfers_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_stock_transfers_locations", "source_location_id <> destination_location_id");
            t.HasCheckConstraint("ck_stock_transfers_transfer_status", "transfer_status IN ('REQUESTED', 'APPROVED', 'SHIPPED', 'PARTIALLY_RECEIVED', 'RECEIVED', 'CANCELLED')");
        });
    }
}