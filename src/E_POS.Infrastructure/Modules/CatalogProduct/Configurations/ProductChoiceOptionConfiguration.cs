using E_POS.Domain.Modules.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class ProductChoiceOptionConfiguration : IEntityTypeConfiguration<ProductChoiceOption>
{
    public void Configure(EntityTypeBuilder<ProductChoiceOption> builder)
    {
        builder.ToTable("product_choice_options");

        builder.HasKey(x => x.Id).HasName("pk_product_choice_options");

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

        builder.Property(x => x.ProductChoiceGroupId)
            .HasColumnName("product_choice_group_id")
            .IsRequired();

        builder.Property(x => x.ChoiceGroupId)
            .HasColumnName("choice_group_id")
            .IsRequired();

        builder.Property(x => x.ChoiceOptionId)
            .HasColumnName("choice_option_id")
            .IsRequired();

        builder.Property(x => x.PriceAdjustmentOverride)
            .HasColumnName("price_adjustment_override")
            .HasColumnType("numeric(18,4)")
            .IsRequired(false);

        builder.Property(x => x.IsDefaultOption)
            .HasColumnName("is_default_option")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsAvailable)
            .HasColumnName("is_available")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.SortOrderOverride)
            .HasColumnName("sort_order_override")
            .IsRequired(false);

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

        builder.HasOne<ProductChoiceGroup>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductChoiceGroupId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_choice_options_product_choice_group_id_product_choice_groups");

        builder.HasOne<ChoiceGroup>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ChoiceGroupId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_choice_options_choice_group_id_choice_groups");

        builder.HasOne<ChoiceOption>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ChoiceOptionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_choice_options_choice_option_id_choice_options");

        builder.HasIndex(x => new { x.TenantId, x.ProductChoiceGroupId, x.ChoiceOptionId })
            .IsUnique()
            .HasDatabaseName("uq_product_choice_options_tenant_id_prod_choice_group_option");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_product_choice_options_tenant_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_product_choice_options_sort_order", "sort_order_override IS NULL OR sort_order_override >= 0")); 
        builder.ToTable(t => t.HasCheckConstraint("ck_product_choice_options_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')")); 
    }
}
