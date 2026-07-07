using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Configurations;

public sealed class RoleTemplateVersionPermissionConfiguration : IEntityTypeConfiguration<RoleTemplateVersionPermission>
{
    public void Configure(EntityTypeBuilder<RoleTemplateVersionPermission> builder)
    {
        builder.ToTable("role_template_version_permissions");

        builder.HasKey(x => x.Id).HasName("pk_role_template_version_permissions");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.RoleTemplateVersionId)
            .HasColumnName("role_template_version_id")
            .IsRequired();

        builder.Property(x => x.PermissionDefinitionId)
            .HasColumnName("permission_id")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.HasOne<RoleTemplateVersion>()
            .WithMany()
            .HasForeignKey(x => x.RoleTemplateVersionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_role_template_version_permissions_role_template_version_id_role_template_versions");

        builder.HasOne<PermissionDefinition>()
            .WithMany()
            .HasForeignKey(x => x.PermissionDefinitionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_role_template_version_permissions_permission_id_permission_definitions");

        builder.HasIndex(x => new { x.RoleTemplateVersionId, x.PermissionDefinitionId })
            .IsUnique()
            .HasDatabaseName("uq_role_template_version_permissions_role_template_version_id_permission_id");
    }
}
