using E_POS.Domain.Modules.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class ProductChoiceGroupConfiguration : IEntityTypeConfiguration<ProductChoiceGroup>
{
    public void Configure(EntityTypeBuilder<ProductChoiceGroup> builder)
    {
        builder.ToTable("product_choice_groups");

        builder.HasKey(x => x.Id).HasName("pk_product_choice_groups");

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

        builder.Property(x => x.ChoiceGroupId)
            .HasColumnName("choice_group_id")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_choice_groups_product_id_products");

        builder.HasOne<ChoiceGroup>()
            .WithMany()
            .HasForeignKey(x => x.ChoiceGroupId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_choice_groups_choice_group_id_choice_groups");

        builder.HasIndex(x => new { x.ProductId, x.ChoiceGroupId })
            .IsUnique()
            .HasDatabaseName("uq_product_choice_groups_product_id_choice_group_id");
    }
}

