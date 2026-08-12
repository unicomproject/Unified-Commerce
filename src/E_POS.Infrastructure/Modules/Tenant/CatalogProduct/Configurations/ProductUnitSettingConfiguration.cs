using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductUnitSettingConfiguration : IEntityTypeConfiguration<ProductUnitSetting>
{
    public void Configure(EntityTypeBuilder<ProductUnitSetting> builder)
    {
        builder.ToTable("product_unit_settings");

        builder.HasKey(x => x.Id).HasName("pk_product_unit_settings");

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

        builder.Property(x => x.UnitModel)
            .HasColumnName("unit_model")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.BaseUomId)
            .HasColumnName("base_uom_id")
            .IsRequired(false);

        builder.Property(x => x.SellingUomId)
            .HasColumnName("selling_uom_id")
            .IsRequired(false);

        builder.Property(x => x.PurchaseUomId)
            .HasColumnName("purchase_uom_id")
            .IsRequired(false);

        builder.Property(x => x.OuterPackUomId)
            .HasColumnName("outer_pack_uom_id")
            .IsRequired(false);

        builder.Property(x => x.ItemsPerPurchaseUnit)
            .HasColumnName("items_per_purchase_unit")
            .HasColumnType("numeric(18,4)")
            .IsRequired(false);

        builder.Property(x => x.PurchaseUnitsPerOuterPack)
            .HasColumnName("purchase_units_per_outer_pack")
            .HasColumnType("numeric(18,4)")
            .IsRequired(false);

        builder.Property(x => x.AllowDecimalQuantity)
            .HasColumnName("allow_decimal_quantity")
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
            .HasConstraintName("fk_product_unit_settings_tenant_id_tenants");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_product_unit_settings_product_id_products");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.BaseUomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_unit_settings_base_uom_id_unit_of_measures");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.SellingUomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_unit_settings_selling_uom_id_unit_of_measures");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.PurchaseUomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_unit_settings_purchase_uom_id_unit_of_measures");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.OuterPackUomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_unit_settings_outer_pack_uom_id_unit_of_measures");

        builder.HasIndex(x => new { x.TenantId, x.ProductId })
            .IsUnique()
            .HasDatabaseName("uq_product_unit_settings_tenant_product");

        builder.ToTable(t => t.HasCheckConstraint("ck_product_unit_settings_unit_model", "unit_model IN ('SINGLE_UNIT', 'MULTIPLE_UNITS')"));
        builder.ToTable(t => t.HasCheckConstraint("ck_product_unit_settings_purchase_factor", "items_per_purchase_unit IS NULL OR items_per_purchase_unit > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_product_unit_settings_outer_pack_factor", "purchase_units_per_outer_pack IS NULL OR purchase_units_per_outer_pack > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_product_unit_settings_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}
