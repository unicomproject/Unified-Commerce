using E_POS.Domain.Modules.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

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

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ProductOptionTemplateId)
            .HasColumnName("product_option_template_id")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order");

        builder.Property(x => x.ValueCode)
            .HasColumnName("value_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.HasOne<ProductOptionTemplate>()
            .WithMany()
            .HasForeignKey(x => x.ProductOptionTemplateId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_option_template_values_product_option_template_id_product_option_templates");

        builder.HasIndex(x => new { x.ProductOptionTemplateId, x.ValueCode })
            .IsUnique()
            .HasDatabaseName("uq_product_option_template_values_product_option_template_id_value_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_product_option_template_values_sort_order", "sort_order >= 0")); 
    }
}

