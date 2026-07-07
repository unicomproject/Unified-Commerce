using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductOptionValueConfiguration : IEntityTypeConfiguration<ProductOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductOptionValue> builder)
    {
        builder.ToTable("product_option_values");

        builder.HasKey(x => x.Id).HasName("pk_product_option_values");

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

        builder.Property(x => x.ProductOptionId)
            .HasColumnName("product_option_id")
            .IsRequired();

        builder.Property(x => x.SourceOptionTemplateValueId)
            .HasColumnName("source_option_template_value_id")
            .IsRequired(false);

        builder.Property(x => x.ValueCode)
            .HasColumnName("value_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.ValueName)
            .HasColumnName("value_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(x => x.ColorHex)
            .HasColumnName("color_hex")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(x => x.ImageUrl)
            .HasColumnName("image_url")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        builder.HasOne<ProductOption>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductOptionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_option_values_product_option_id_product_options");

        builder.HasOne<ProductOptionTemplateValue>()
            .WithMany()
            .HasForeignKey(x => x.SourceOptionTemplateValueId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_option_values_source_option_template_value_id_product_option_template_values");

        builder.HasIndex(x => new { x.TenantId, x.ProductOptionId, x.ValueCode })
            .IsUnique()
            .HasDatabaseName("uq_product_option_values_tenant_id_product_option_id_value_code");

        builder.HasIndex(x => new { x.TenantId, x.ProductOptionId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_product_option_values_tenant_id_product_option_id_id");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_product_option_values_tenant_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_product_option_values_sort_order", "sort_order >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_product_option_values_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}


