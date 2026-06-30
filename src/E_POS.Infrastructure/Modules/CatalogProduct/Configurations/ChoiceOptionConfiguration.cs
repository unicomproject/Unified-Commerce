using E_POS.Domain.Modules.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class ChoiceOptionConfiguration : IEntityTypeConfiguration<ChoiceOption>
{
    public void Configure(EntityTypeBuilder<ChoiceOption> builder)
    {
        builder.ToTable("choice_options");

        builder.HasKey(x => x.Id).HasName("pk_choice_options");

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

        builder.Property(x => x.ChoiceGroupId)
            .HasColumnName("choice_group_id")
            .IsRequired();

        builder.Property(x => x.OptionCode)
            .HasColumnName("option_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order");

        builder.HasOne<ChoiceGroup>()
            .WithMany()
            .HasForeignKey(x => x.ChoiceGroupId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_choice_options_choice_group_id_choice_groups");

        builder.HasIndex(x => new { x.ChoiceGroupId, x.OptionCode })
            .IsUnique()
            .HasDatabaseName("uq_choice_options_choice_group_id_option_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_choice_options_sort_order", "sort_order >= 0")); 
    }
}

