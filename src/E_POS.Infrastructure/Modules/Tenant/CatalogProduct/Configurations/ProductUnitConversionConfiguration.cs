using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductUnitConversionConfiguration : IEntityTypeConfiguration<ProductUnitConversion>
{
    public void Configure(EntityTypeBuilder<ProductUnitConversion> builder)
    {
        builder.ToTable("product_unit_conversions");

        builder.HasKey(x => x.Id).HasName("pk_product_unit_conversions");

        builder.Property(x => x.Id).HasColumnName("id");

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

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.UomId)
            .HasColumnName("uom_id")
            .IsRequired();

        builder.Property(x => x.UnitLevel)
            .HasColumnName("unit_level")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ConversionToBaseFactor)
            .HasColumnName("conversion_to_base_factor")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.IsBaseUnit)
            .HasColumnName("is_base_unit")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsSellingUnit)
            .HasColumnName("is_selling_unit")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsPurchaseUnit)
            .HasColumnName("is_purchase_unit")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsOuterPackUnit)
            .HasColumnName("is_outer_pack_unit")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasDefaultValue("ACTIVE")
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_unit_conversions_tenant_id_tenants");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_product_unit_conversions_product_id_products");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.UomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_unit_conversions_uom_id_unit_of_measures");

        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.UomId })
            .IsUnique()
            .HasDatabaseName("uq_product_unit_conversions_tenant_product_uom");

        builder.HasIndex(x => new { x.TenantId, x.ProductId })
            .HasDatabaseName("idx_product_unit_conversions_tenant_product");

        builder.ToTable(t => t.HasCheckConstraint("ck_product_unit_conversions_level", "unit_level IN ('BASE', 'SELLING', 'PURCHASE', 'OUTER_PACK')"));
        builder.ToTable(t => t.HasCheckConstraint("ck_product_unit_conversions_factor", "conversion_to_base_factor > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_product_unit_conversions_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}
