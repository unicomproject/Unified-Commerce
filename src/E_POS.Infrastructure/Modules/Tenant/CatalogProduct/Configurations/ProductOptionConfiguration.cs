using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
{
    public void Configure(EntityTypeBuilder<ProductOption> builder)
    {
        builder.ToTable("product_options");

        builder.HasKey(x => x.Id).HasName("pk_product_options");

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

        builder.Property(x => x.SourceOptionTemplateId)
            .HasColumnName("source_option_template_id")
            .IsRequired(false);

        builder.Property(x => x.OptionCode)
            .HasColumnName("option_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.OptionName)
            .HasColumnName("option_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.OptionType)
            .HasColumnName("option_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.InputType)
            .HasColumnName("input_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.IsRequired)
            .HasColumnName("is_required")
            .HasDefaultValue(true)
            .IsRequired();

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

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_options_product_id_products");

        builder.HasOne<ProductOptionTemplate>()
            .WithMany()
            .HasForeignKey(x => x.SourceOptionTemplateId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_options_source_option_template_id_product_option_templates");

        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.OptionCode })
            .IsUnique()
            .HasDatabaseName("uq_product_options_tenant_id_product_id_option_code");

        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_product_options_tenant_id_product_id_id");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_product_options_tenant_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_product_options_sort_order", "sort_order >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_product_options_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}


