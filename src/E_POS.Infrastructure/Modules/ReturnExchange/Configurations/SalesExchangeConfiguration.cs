using E_POS.Domain.Modules.Orders.Entities;
using E_POS.Domain.Modules.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ReturnExchange.Configurations;

public sealed class SalesExchangeConfiguration : IEntityTypeConfiguration<SalesExchange>
{
    public void Configure(EntityTypeBuilder<SalesExchange> builder)
    {
        builder.ToTable("sales_exchanges");

        builder.HasKey(x => x.Id).HasName("pk_sales_exchanges");

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
            .HasColumnName("tenant_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.AdditionalAmount)
            .HasColumnName("additional_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.ExchangeNumber)
            .HasColumnName("exchange_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.RefundAmount)
            .HasColumnName("refund_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.ReplacementOrderId)
            .HasColumnName("replacement_order_id")
            .IsRequired();

        builder.Property(x => x.SalesReturnId)
            .HasColumnName("sales_return_id")
            .IsRequired(false);

        builder.HasOne<SalesReturn>()
            .WithMany()
            .HasForeignKey(x => x.SalesReturnId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchanges_sales_return_id_sales_returns");

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.ReplacementOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchanges_replacement_order_id_sales_orders");

        builder.HasIndex(x => new { x.TenantId, x.ExchangeNumber })
            .IsUnique()
            .HasDatabaseName("uq_sales_exchanges_tenant_id_exchange_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_exchanges_additional_amount", "additional_amount >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_exchanges_refund_amount", "refund_amount >= 0")); 
    }
}

