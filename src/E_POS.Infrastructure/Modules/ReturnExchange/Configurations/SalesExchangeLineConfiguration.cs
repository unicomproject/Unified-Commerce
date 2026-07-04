using E_POS.Domain.Modules.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ReturnExchange.Configurations;

public sealed class SalesExchangeLineConfiguration : IEntityTypeConfiguration<SalesExchangeLine>
{
    public void Configure(EntityTypeBuilder<SalesExchangeLine> builder)
    {
        builder.ToTable("sales_exchange_lines");

        builder.HasKey(x => x.Id).HasName("pk_sales_exchange_lines");

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

        builder.Property(x => x.SalesExchangeId)
            .HasColumnName("sales_exchange_id")
            .IsRequired();

        builder.Property(x => x.SalesReturnLineId)
            .HasColumnName("sales_return_line_id")
            .IsRequired(false);

        builder.Property(x => x.ReplacementProductId)
            .HasColumnName("replacement_product_id")
            .IsRequired(false);

        builder.Property(x => x.ReplacementProductVariantId)
            .HasColumnName("replacement_product_variant_id")
            .IsRequired(false);

        builder.Property(x => x.ReplacementSalesOrderLineId)
            .HasColumnName("replacement_sales_order_line_id")
            .IsRequired(false);

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.OriginalLineAmount)
            .HasColumnName("original_line_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.ReplacementLineAmount)
            .HasColumnName("replacement_line_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.NetDifferenceAmount)
            .HasColumnName("net_difference_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.ExchangeActionType)
            .HasColumnName("exchange_action_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        // <second-brain-constraints>
        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchange_lines_6a490826");

        builder.HasOne<E_POS.Domain.Modules.ReturnExchange.Entities.SalesExchange>()
            .WithMany()
            .HasForeignKey(x => x.SalesExchangeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchange_lines_aacc4f1a");

        builder.HasOne<E_POS.Domain.Modules.ReturnExchange.Entities.SalesReturnLine>()
            .WithMany()
            .HasForeignKey(x => x.SalesReturnLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchange_lines_b203b3aa");

        builder.HasOne<E_POS.Domain.Modules.CatalogProduct.Entities.Product>()
            .WithMany()
            .HasForeignKey(x => x.ReplacementProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchange_lines_c7ddff73");

        builder.HasOne<E_POS.Domain.Modules.CatalogProduct.Entities.ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.ReplacementProductVariantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchange_lines_2fc89440");

        builder.HasOne<E_POS.Domain.Modules.Orders.Entities.SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => x.ReplacementSalesOrderLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchange_lines_95402dad");
        // </second-brain-constraints>
    }
}