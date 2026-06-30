using E_POS.Domain.Modules.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.AccessControl.Configurations;

public sealed class RoleTemplateVersionConfiguration : IEntityTypeConfiguration<RoleTemplateVersion>
{
    public void Configure(EntityTypeBuilder<RoleTemplateVersion> builder)
    {
        builder.ToTable("role_template_versions");

        builder.HasKey(x => x.Id).HasName("pk_role_template_versions");

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

        builder.Property(x => x.RoleTemplateId)
            .HasColumnName("role_template_id")
            .IsRequired();

        builder.Property(x => x.VersionNumber)
            .HasColumnName("version_number");

        builder.HasOne<RoleTemplate>()
            .WithMany()
            .HasForeignKey(x => x.RoleTemplateId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_role_template_versions_role_template_id_role_templates");

        builder.HasIndex(x => new { x.RoleTemplateId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("uq_role_template_versions_role_template_id_version_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_role_template_versions_version_number", "version_number > 0")); 
    }
}

