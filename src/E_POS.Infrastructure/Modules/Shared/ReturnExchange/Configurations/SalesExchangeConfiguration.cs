using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Configurations;

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
            .IsRequired(false);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.DocumentNumberSequenceId)
            .HasColumnName("document_number_sequence_id")
            .IsRequired(false);

        builder.Property(x => x.SalesReturnId)
            .HasColumnName("sales_return_id")
            .IsRequired(false);

        builder.Property(x => x.ReplacementSalesOrderId)
            .HasColumnName("replacement_sales_order_id")
            .IsRequired(false);

        builder.Property(x => x.ExchangeNumber)
            .HasColumnName("exchange_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.ExchangeStatus)
            .HasColumnName("exchange_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ExchangeMode)
            .HasColumnName("exchange_mode")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.PriceDifferenceAmount)
            .HasColumnName("price_difference_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.AdditionalPaymentAmount)
            .HasColumnName("additional_payment_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.RefundBackAmount)
            .HasColumnName("refund_back_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.CancelledAt)
            .HasColumnName("cancelled_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.ExchangeNumber })
            .IsUnique()
            .HasDatabaseName("ux_sales_exchanges_3413dbba");
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL")
            .HasDatabaseName("ux_sales_exchanges_tenant_idempotency_key");
        builder.HasIndex(x => new { x.TenantId, x.SalesReturnId })
            .IsUnique()
            .HasFilter("sales_return_id IS NOT NULL")
            .HasDatabaseName("ux_sales_exchanges_sales_return");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchanges_42236def");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.DocumentNumberSequence>()
            .WithMany()
            .HasForeignKey(x => x.DocumentNumberSequenceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchanges_f66bba6d");

        builder.HasOne<E_POS.Domain.Modules.Shared.ReturnExchange.Entities.SalesReturn>()
            .WithMany()
            .HasForeignKey(x => x.SalesReturnId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchanges_c5dff21b");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.ReplacementSalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchanges_c51ae4c1");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchanges_1735e151");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchanges_86babc7e");
        // </second-brain-constraints>
    }
}

