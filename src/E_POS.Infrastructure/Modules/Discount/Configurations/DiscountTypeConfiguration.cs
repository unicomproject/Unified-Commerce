using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Discount.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Discount.Configurations;
public sealed class DiscountTypeConfiguration : IEntityTypeConfiguration<DiscountType>
{
    public void Configure(EntityTypeBuilder<DiscountType> builder)
    {
        builder.ToTable("discount_types");
        builder.HasKey(x => x.Id).HasName("pk_discount_types");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.UpdatedBy);
        builder.Property(x => x.DiscountTypeCode).HasColumnName("discount_type_code").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.DiscountTypeName).HasColumnName("discount_type_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.CalculationMethod).HasColumnName("calculation_method").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.IsSystemType).HasColumnName("is_system_type").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.DiscountTypeCode).IsUnique().HasDatabaseName("uq_discount_types_discount_type_code");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_discount_types_calculation_method", "calculation_method IN ('PERCENTAGE', 'FIXED_AMOUNT', 'BUY_X_GET_Y', 'FREE_ITEM', 'PRICE_OVERRIDE')");
            t.HasCheckConstraint("ck_discount_types_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        });
    }
}