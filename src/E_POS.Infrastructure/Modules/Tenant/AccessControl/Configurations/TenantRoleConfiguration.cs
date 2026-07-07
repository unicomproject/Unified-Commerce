using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Configurations;

public sealed class TenantRoleConfiguration : IEntityTypeConfiguration<TenantRole>
{
    public void Configure(EntityTypeBuilder<TenantRole> builder)
    {
        builder.ToTable("tenant_roles");

        builder.HasKey(x => x.Id).HasName("pk_tenant_roles");

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

        builder.Property(x => x.SourceRoleTemplateId)
            .HasColumnName("source_role_template_id")
            .IsRequired(false);

        builder.Property(x => x.SourceRoleTemplateVersionId)
            .HasColumnName("source_role_template_version_id")
            .IsRequired(false);

        builder.Property(x => x.RoleName)
            .HasColumnName("role_name")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.RoleCode)
            .HasColumnName("role_code")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RoleDescription)
            .HasColumnName("role_description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.IsCustom)
            .HasColumnName("is_custom")
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired();

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_roles_tenant_id_tenants");

        builder.HasOne<RoleTemplate>()
            .WithMany()
            .HasForeignKey(x => x.SourceRoleTemplateId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_roles_source_role_template_id_role_templates");

        builder.HasOne<RoleTemplateVersion>()
            .WithMany()
            .HasForeignKey(x => x.SourceRoleTemplateVersionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_roles_source_role_template_version_id_role_template_versions");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_roles_created_by");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_roles_updated_by");

        builder.HasIndex(x => new { x.TenantId, x.RoleCode })
            .IsUnique()
            .HasDatabaseName("uq_tenant_roles_tenant_id_role_code");

        builder.HasIndex(x => new { x.TenantId, x.RoleName })
            .IsUnique()
            .HasDatabaseName("uq_tenant_roles_tenant_id_role_name");
    }
}
