using E_POS.Domain.Modules.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class ComboDefinitionConfiguration : IEntityTypeConfiguration<ComboDefinition>
{
    public void Configure(EntityTypeBuilder<ComboDefinition> builder)
    {
        builder.ToTable("combo_definitions");

        builder.HasKey(x => x.Id).HasName("pk_combo_definitions");

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

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id")
            .IsRequired(false);

        builder.Property(x => x.ComboCode)
            .HasColumnName("combo_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.ComboName)
            .HasColumnName("combo_name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PricingMode)
            .HasColumnName("pricing_mode")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.InventoryDeductionMode)
            .HasColumnName("inventory_deduction_mode")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_definitions_product_id_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_definitions_product_variant_id_product_variants");

        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.ComboCode })
            .IsUnique()
            .HasDatabaseName("uq_combo_definitions_tenant_id_product_id_combo_code")
            .HasFilter("product_variant_id IS NULL");

        builder.HasIndex(x => new { x.TenantId, x.ProductVariantId, x.ComboCode })
            .IsUnique()
            .HasDatabaseName("uq_combo_definitions_tenant_id_product_variant_id_combo_code")
            .HasFilter("product_variant_id IS NOT NULL");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_combo_definitions_tenant_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_combo_definitions_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}
