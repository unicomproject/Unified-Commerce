using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Configurations;

public sealed class PosOrderHoldConfiguration : IEntityTypeConfiguration<PosOrderHold>
{
    public void Configure(EntityTypeBuilder<PosOrderHold> builder)
    {
        builder.ToTable("pos_order_holds");

        builder.HasKey(x => x.Id).HasName("pk_pos_order_holds");

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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.HoldNumber)
            .HasColumnName("hold_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired();

        builder.Property(x => x.HoldStatus)
            .HasColumnName("hold_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.HoldReason)
            .HasColumnName("hold_reason")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250);

        builder.Property(x => x.HeldByTenantUserId)
            .HasColumnName("held_by_tenant_user_id")
            .IsRequired();

        builder.Property(x => x.HeldAt)
            .HasColumnName("held_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ReleasedByTenantUserId)
            .HasColumnName("released_by_tenant_user_id");

        builder.Property(x => x.ReleasedAt)
            .HasColumnName("released_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CancelledAt)
            .HasColumnName("cancelled_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pos_order_holds_tenant_id_tenants");

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pos_order_holds_sales_order_id_sales_orders");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.HeldByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pos_order_holds_held_by_tenant_user_id_tenant_users");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.ReleasedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pos_order_holds_released_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.HoldNumber })
            .IsUnique()
            .HasDatabaseName("uq_pos_order_holds_tenant_id_hold_number");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_pos_order_holds_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_pos_order_holds_hold_status", "hold_status IN ('HELD', 'RELEASED', 'EXPIRED', 'CANCELLED')");
        });
    }
}



