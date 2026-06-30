using E_POS.Domain.Modules.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class ComboGroupConfiguration : IEntityTypeConfiguration<ComboGroup>
{
    public void Configure(EntityTypeBuilder<ComboGroup> builder)
    {
        builder.ToTable("combo_groups");

        builder.HasKey(x => x.Id).HasName("pk_combo_groups");

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

        builder.Property(x => x.ComboDefinitionId)
            .HasColumnName("combo_definition_id")
            .IsRequired();

        builder.Property(x => x.GroupCode)
            .HasColumnName("group_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.MaxSelect)
            .HasColumnName("max_select");

        builder.Property(x => x.MinSelect)
            .HasColumnName("min_select");

        builder.HasOne<ComboDefinition>()
            .WithMany()
            .HasForeignKey(x => x.ComboDefinitionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_groups_combo_definition_id_combo_definitions");

        builder.HasIndex(x => new { x.ComboDefinitionId, x.GroupCode })
            .IsUnique()
            .HasDatabaseName("uq_combo_groups_combo_definition_id_group_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_combo_groups_min_select", "min_select >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_combo_groups_max_select_min_select", "max_select >= min_select")); 
    }
}

