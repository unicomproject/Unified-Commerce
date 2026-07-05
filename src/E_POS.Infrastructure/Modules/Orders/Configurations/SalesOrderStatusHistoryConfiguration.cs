using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.Orders.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Orders.Configurations;

public sealed class SalesOrderStatusHistoryConfiguration : IEntityTypeConfiguration<SalesOrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<SalesOrderStatusHistory> builder)
    {
        builder.ToTable("sales_order_status_history");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_status_history");

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

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired();

        builder.Property(x => x.SequenceNumber)
            .HasColumnName("sequence_number")
            .IsRequired();

        builder.Property(x => x.StatusType)
            .HasColumnName("status_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.OldStatus)
            .HasColumnName("old_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.NewStatus)
            .HasColumnName("new_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ChangedByTenantUserId)
            .HasColumnName("changed_by_tenant_user_id");

        builder.Property(x => x.ChangedAt)
            .HasColumnName("changed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ChangeReason)
            .HasColumnName("change_reason")
            .HasColumnType("text");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_status_history_tenant_id_tenants");

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_status_history_sales_order_id_sales_orders");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.ChangedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_status_history_changed_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.SalesOrderId, x.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("uq_sales_order_status_history_sales_order_id_sequence_number");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_sales_order_status_history_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_sales_order_status_history_sequence_number", "sequence_number > 0");
            t.HasCheckConstraint("ck_sales_order_status_history_status_type", "status_type IN ('ORDER_STATUS', 'PAYMENT_STATUS', 'FULFILLMENT_STATUS')");
        });
    }
}
