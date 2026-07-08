using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Configurations;

public sealed class SalesReturnLineConfiguration : IEntityTypeConfiguration<SalesReturnLine>
{
    public void Configure(EntityTypeBuilder<SalesReturnLine> builder)
    {
        builder.ToTable("sales_return_lines");

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
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_return_lines_5de350c6");

        builder.HasOne<E_POS.Domain.Modules.Shared.ReturnExchange.Entities.SalesReturn>()
            .WithMany()
            .HasForeignKey(x => x.SalesReturnId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_return_lines_6cf63e7f");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_return_lines_1e6282f1");

        builder.HasOne<E_POS.Domain.Modules.Shared.ReturnExchange.Entities.ReturnReason>()
            .WithMany()
            .HasForeignKey(x => x.ReturnReasonId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_return_lines_4427f485");
        // </second-brain-constraints>
    }
}

