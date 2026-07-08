using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Orders.Configurations;

public sealed class SalesOrderLineStatusHistoryConfiguration : IEntityTypeConfiguration<SalesOrderLineStatusHistory>
{
    public void Configure(EntityTypeBuilder<SalesOrderLineStatusHistory> builder)
    {
        builder.ToTable("sales_order_line_status_history");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_line_status_history");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Ignore(x => x.CreatedAt);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.SalesOrderLineId)
            .HasColumnName("sales_order_line_id")
            .IsRequired();

        builder.Property(x => x.SequenceNumber)
            .HasColumnName("sequence_number")
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

        builder.Property(x => x.AffectedQuantity)
            .HasColumnName("affected_quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.ChangedByTenantUserId)
            .HasColumnName("changed_by_tenant_user_id");

        builder.Property(x => x.ChangedAt)
            .HasColumnName("changed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ChangeReason)
            .HasColumnName("change_reason")
            .HasColumnType("text");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_status_history_tenant_id_tenants");

        builder.HasOne<SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderLineId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_status_history_sales_order_line_id_sales_order_lines");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.ChangedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_status_history_changed_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.SalesOrderLineId, x.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("uq_sales_order_line_status_history_sales_order_line_id_sequence_number");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_sales_order_line_status_history_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_sales_order_line_status_history_sequence_number", "sequence_number > 0");
            t.HasCheckConstraint("ck_sales_order_line_status_history_affected_quantity", "affected_quantity IS NULL OR affected_quantity >= 0");
        });
    }
}



