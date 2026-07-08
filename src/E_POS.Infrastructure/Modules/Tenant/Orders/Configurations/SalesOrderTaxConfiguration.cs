using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Orders.Configurations;

public sealed class SalesOrderTaxConfiguration : IEntityTypeConfiguration<SalesOrderTax>
{
    public void Configure(EntityTypeBuilder<SalesOrderTax> builder)
    {
        builder.ToTable("sales_order_taxes");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_taxes");

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

        builder.Property(x => x.TaxJurisdictionId)
            .HasColumnName("tax_jurisdiction_id");

        builder.Property(x => x.TaxClassId)
            .HasColumnName("tax_class_id");

        builder.Property(x => x.TaxRateId)
            .HasColumnName("tax_rate_id");

        builder.Property(x => x.TaxClassCodeSnapshot)
            .HasColumnName("tax_class_code_snapshot")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.TaxRateCodeSnapshot)
            .HasColumnName("tax_rate_code_snapshot")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.TaxNameSnapshot)
            .HasColumnName("tax_name_snapshot")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.JurisdictionNameSnapshot)
            .HasColumnName("jurisdiction_name_snapshot")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.TaxRatePercent)
            .HasColumnName("tax_rate_percent")
            .HasPrecision(7, 4)
            .IsRequired();

        builder.Property(x => x.TaxableAmount)
            .HasColumnName("taxable_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.TaxAmount)
            .HasColumnName("tax_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.IsTaxIncluded)
            .HasColumnName("is_tax_included")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CalculationSequence)
            .HasColumnName("calculation_sequence")
            .IsRequired()
            .HasDefaultValue(1);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_taxes_tenant_id_tenants");

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_taxes_sales_order_id_sales_orders");

        builder.HasOne<SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderLineId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_taxes_sales_order_line_id_sales_order_lines");

        builder.HasOne<TaxJurisdiction>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TaxJurisdictionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_taxes_tax_jurisdiction_id_tax_jurisdictions");

        builder.HasOne<TaxClass>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TaxClassId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_taxes_tax_class_id_tax_classes");

        builder.HasOne<TaxRate>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TaxRateId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_taxes_tax_rate_id_tax_rates");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_sales_order_taxes_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_sales_order_taxes_tax_rate_percent", "tax_rate_percent >= 0 AND tax_rate_percent <= 100");
            t.HasCheckConstraint("ck_sales_order_taxes_taxable_amount", "taxable_amount >= 0");
            t.HasCheckConstraint("ck_sales_order_taxes_tax_amount", "tax_amount >= 0");
            t.HasCheckConstraint("ck_sales_order_taxes_calculation_sequence", "calculation_sequence > 0");
        });
    }
}



