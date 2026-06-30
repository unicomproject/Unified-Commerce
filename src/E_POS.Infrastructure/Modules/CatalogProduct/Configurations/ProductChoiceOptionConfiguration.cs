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

        builder.Property(x => x.ChoiceOptionId)
            .HasColumnName("choice_option_id")
            .IsRequired();

        builder.Property(x => x.ProductChoiceGroupId)
            .HasColumnName("product_choice_group_id")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne<ProductChoiceGroup>()
            .WithMany()
            .HasForeignKey(x => x.ProductChoiceGroupId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_choice_options_product_choice_group_id_product_choice_groups");

        builder.HasOne<ChoiceOption>()
            .WithMany()
            .HasForeignKey(x => x.ChoiceOptionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_choice_options_choice_option_id_choice_options");

        builder.HasIndex(x => new { x.ProductChoiceGroupId, x.ChoiceOptionId })
            .IsUnique()
            .HasDatabaseName("uq_product_choice_options_product_choice_group_id_choice_option_id");
    }
}

