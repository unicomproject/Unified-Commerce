using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class BusinessTypeOptionTemplateConfiguration : IEntityTypeConfiguration<BusinessTypeOptionTemplate>
{
    public void Configure(EntityTypeBuilder<BusinessTypeOptionTemplate> builder)
    {
        builder.ToTable("business_type_option_templates");

        builder.HasKey(x => x.Id).HasName("pk_business_type_option_templates");

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

        builder.Property(x => x.BusinessTypeId)
            .HasColumnName("business_type_id")
            .IsRequired();

        builder.Property(x => x.OptionTemplateId)
            .HasColumnName("option_template_id")
            .IsRequired();

        builder.Property(x => x.IsDefaultTemplate)
            .HasColumnName("is_default_template")
            .HasDefaultValue(false)
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

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByPlatformUserId)
            .HasColumnName("updated_by_platform_user_id")
            .IsRequired(false);

        builder.HasOne<BusinessType>()
            .WithMany()
            .HasForeignKey(x => x.BusinessTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_business_type_option_templates_business_type_id_business_types");

        builder.HasOne<ProductOptionTemplate>()
            .WithMany()
            .HasForeignKey(x => x.OptionTemplateId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_business_type_option_templates_option_template_id_product_option_templates");

        builder.HasIndex(x => new { x.BusinessTypeId, x.OptionTemplateId })
            .IsUnique()
            .HasDatabaseName("uq_business_type_option_templates_business_type_id_option_template_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_business_type_option_templates_sort_order", "sort_order >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_business_type_option_templates_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}


