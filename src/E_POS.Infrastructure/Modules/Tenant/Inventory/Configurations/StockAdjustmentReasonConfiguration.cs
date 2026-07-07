using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Configurations;

public sealed class StockAdjustmentReasonConfiguration : IEntityTypeConfiguration<StockAdjustmentReason>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentReason> builder)
    {
        builder.ToTable("stock_adjustment_reasons");

        builder.HasKey(x => x.Id).HasName("pk_stock_adjustment_reasons");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ReasonCode).HasColumnName("reason_code").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.ReasonName).HasColumnName("reason_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Direction).HasColumnName("direction").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.RequiresManagerApproval).HasColumnName("requires_manager_approval").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.IsSystemReason).HasColumnName("is_system_reason").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_adjustment_reasons_tenant_id_tenants");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_adjustment_reasons_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_adjustment_reasons_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.ReasonCode }).IsUnique().HasDatabaseName("uq_stock_adjustment_reasons_tenant_id_reason_code");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_stock_adjustment_reasons_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_stock_adjustment_reasons_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        });
    }
}


