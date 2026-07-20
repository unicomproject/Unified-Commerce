using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Configurations;

public sealed class SalesReturnLineConfiguration : IEntityTypeConfiguration<SalesReturnLine>
{
    public void Configure(EntityTypeBuilder<SalesReturnLine> builder)
    {
        builder.ToTable("sales_return_lines", t =>
        {
            t.HasCheckConstraint(
                "ck_sales_return_lines_quantity_requested",
                "quantity_requested > 0");
            t.HasCheckConstraint(
                "ck_sales_return_lines_quantity_received",
                "quantity_received IS NULL OR quantity_received >= 0");
            t.HasCheckConstraint(
                "ck_sales_return_lines_quantity_received_lte_requested",
                "quantity_received IS NULL OR quantity_received <= quantity_requested");
            t.HasCheckConstraint(
                "ck_sales_return_lines_unit_price_snapshot",
                "unit_price_snapshot >= 0");
            t.HasCheckConstraint(
                "ck_sales_return_lines_unit_tax_amount_snapshot",
                "unit_tax_amount_snapshot >= 0");
            t.HasCheckConstraint(
                "ck_sales_return_lines_line_subtotal_amount",
                "line_subtotal_amount >= 0");
            t.HasCheckConstraint(
                "ck_sales_return_lines_line_tax_amount",
                "line_tax_amount >= 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_sales_return_lines");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.SalesReturnId)
            .HasColumnName("sales_return_id")
            .IsRequired();

        builder.Property(x => x.SalesOrderLineId)
            .HasColumnName("sales_order_line_id")
            .IsRequired();

        builder.Property(x => x.ReturnReasonId)
            .HasColumnName("return_reason_id")
            .IsRequired(false);

        builder.Property(x => x.QuantityRequested)
            .HasColumnName("quantity_requested")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.QuantityReceived)
            .HasColumnName("quantity_received")
            .HasPrecision(18, 4)
            .IsRequired(false);

        builder.Property(x => x.UnitPriceSnapshot)
            .HasColumnName("unit_price_snapshot")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.UnitTaxAmountSnapshot)
            .HasColumnName("unit_tax_amount_snapshot")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.LineSubtotalAmount)
            .HasColumnName("line_subtotal_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.LineTaxAmount)
            .HasColumnName("line_tax_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.DispositionStatus)
            .HasColumnName("disposition_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasColumnType("text")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_sales_return_lines_tenant_id_id");

        builder.HasIndex(x => new { x.TenantId, x.SalesReturnId, x.SalesOrderLineId })
            .IsUnique()
            .HasDatabaseName("uq_sales_return_lines_tenant_return_order_line");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_return_lines_5de350c6");

        builder.HasOne<E_POS.Domain.Modules.Shared.ReturnExchange.Entities.SalesReturn>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesReturnId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_return_lines_return_tenant");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderLineId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_return_lines_order_line_tenant");

        builder.HasOne<E_POS.Domain.Modules.Shared.ReturnExchange.Entities.ReturnReason>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReturnReasonId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_return_lines_return_reason_tenant");
        // </second-brain-constraints>
    }
}
