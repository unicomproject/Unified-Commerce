using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Orders.Configurations;

public sealed class SalesOrderChargeConfiguration : IEntityTypeConfiguration<SalesOrderCharge>
{
    public void Configure(EntityTypeBuilder<SalesOrderCharge> builder)
    {
        builder.ToTable("sales_order_charges");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_charges");

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

        builder.Property(x => x.SalesOrderLineId)
            .HasColumnName("sales_order_line_id");

        builder.Property(x => x.ChargeScope)
            .HasColumnName("charge_scope")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ChargeType)
            .HasColumnName("charge_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ChargeNameSnapshot)
            .HasColumnName("charge_name_snapshot")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.ChargeAmount)
            .HasColumnName("charge_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.IsTaxable)
            .HasColumnName("is_taxable")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.AppliedByTenantUserId)
            .HasColumnName("applied_by_tenant_user_id");

        builder.Property(x => x.AppliedAt)
            .HasColumnName("applied_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_charges_tenant_id_tenants");

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_charges_sales_order_id_sales_orders");

        builder.HasOne<SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderLineId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_charges_sales_order_line_id_sales_order_lines");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.AppliedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_charges_applied_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_sales_order_charges_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_sales_order_charges_charge_scope", "charge_scope IN ('ORDER', 'LINE')");
            t.HasCheckConstraint("ck_sales_order_charges_charge_type", "charge_type IN ('SERVICE_FEE', 'PACKAGING_FEE', 'ROUNDING', 'OTHER')");
            t.HasCheckConstraint("ck_sales_order_charges_charge_amount", "charge_amount >= 0");
        });
    }
}



