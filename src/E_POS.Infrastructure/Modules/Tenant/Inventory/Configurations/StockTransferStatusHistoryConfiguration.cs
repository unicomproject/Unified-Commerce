using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Configurations;

public sealed class StockTransferStatusHistoryConfiguration : IEntityTypeConfiguration<StockTransferStatusHistory>
{
    public void Configure(EntityTypeBuilder<StockTransferStatusHistory> builder)
    {
        builder.ToTable("stock_transfer_status_history");

        builder.HasKey(x => x.Id).HasName("pk_stock_transfer_status_history");

        builder.Property(x => x.Id).HasColumnName("id");
        
        builder.Ignore(x => x.CreatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.StockTransferId).HasColumnName("stock_transfer_id").IsRequired();
        builder.Property(x => x.SequenceNumber).HasColumnName("sequence_number").IsRequired();
        builder.Property(x => x.OldStatus).HasColumnName("old_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired(false);
        builder.Property(x => x.NewStatus).HasColumnName("new_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ChangedByTenantUserId).HasColumnName("changed_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.ChangedAt).HasColumnName("changed_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ChangeReason).HasColumnName("change_reason").HasColumnType("text").IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfer_status_history_tenant_id_tenants");
        
        builder.HasOne<StockTransfer>().WithMany().HasForeignKey(x => new { x.TenantId, x.StockTransferId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfer_status_history_stock_transfer_id_stock_transfers");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.ChangedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfer_status_history_changed_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.StockTransferId, x.SequenceNumber }).IsUnique().HasDatabaseName("uq_stock_transfer_status_history_tenant_id_stock_transfer_id_sequence_number");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_stock_transfer_status_history_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_stock_transfer_status_history_sequence_number", "sequence_number > 0");
        });
    }
}


