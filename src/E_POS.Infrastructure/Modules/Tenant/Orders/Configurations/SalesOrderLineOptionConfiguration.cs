using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Orders.Configurations;

public sealed class SalesOrderLineOptionConfiguration : IEntityTypeConfiguration<SalesOrderLineOption>
{
    public void Configure(EntityTypeBuilder<SalesOrderLineOption> builder)
    {
        builder.ToTable("sales_order_line_options");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_line_options");

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

        builder.Property(x => x.SalesOrderLineId)
            .HasColumnName("sales_order_line_id")
            .IsRequired();

        builder.Property(x => x.ProductChoiceGroupId)
            .HasColumnName("product_choice_group_id");

        builder.Property(x => x.ProductChoiceOptionId)
            .HasColumnName("product_choice_option_id");

        builder.Property(x => x.ChoiceGroupNameSnapshot)
            .HasColumnName("choice_group_name_snapshot")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.ChoiceOptionNameSnapshot)
            .HasColumnName("choice_option_name_snapshot")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(x => x.UnitPriceAdjustment)
            .HasColumnName("unit_price_adjustment")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.TotalPriceAdjustment)
            .HasColumnName("total_price_adjustment")
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_options_tenant_id_tenants");

        builder.HasOne<SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderLineId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_options_sales_order_line_id_sales_order_lines");

        builder.HasOne<ProductChoiceGroup>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductChoiceGroupId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_options_product_choice_group_id_product_choice_groups");

        builder.HasOne<ProductChoiceOption>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductChoiceOptionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_line_options_product_choice_option_id_product_choice_options");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_sales_order_line_options_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_sales_order_line_options_quantity", "quantity > 0");
            t.HasCheckConstraint("ck_sales_order_line_options_sort_order", "sort_order >= 0");
        });
    }
}



