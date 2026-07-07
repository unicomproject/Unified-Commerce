using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductOptionTemplateValueConfiguration : IEntityTypeConfiguration<ProductOptionTemplateValue>
{
    public void Configure(EntityTypeBuilder<ProductOptionTemplateValue> builder)
    {
        builder.ToTable("product_option_template_values");

        builder.HasKey(x => x.Id).HasName("pk_product_option_template_values");

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

        builder.Property(x => x.OptionTemplateId)
            .HasColumnName("option_template_id")
            .IsRequired();

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
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
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

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByPlatformUserId)
            .HasColumnName("updated_by_platform_user_id")
            .IsRequired(false);

        builder.HasOne<ProductOptionTemplate>()
            .WithMany()
            .HasForeignKey(x => x.OptionTemplateId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_option_template_values_option_template_id_product_option_templates");

        builder.HasIndex(x => new { x.OptionTemplateId, x.ValueCode })
            .IsUnique()
            .HasDatabaseName("uq_product_option_template_values_option_template_id_value_code");

        builder.HasIndex(x => new { x.OptionTemplateId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_product_option_template_values_option_template_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_product_option_template_values_sort_order", "sort_order >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_product_option_template_values_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}


