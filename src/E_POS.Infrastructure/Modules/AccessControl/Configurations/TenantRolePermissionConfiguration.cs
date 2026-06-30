using E_POS.Domain.Modules.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.AccessControl.Configurations;

public sealed class TenantRolePermissionConfiguration : IEntityTypeConfiguration<TenantRolePermission>
{
    public void Configure(EntityTypeBuilder<TenantRolePermission> builder)
    {
        builder.ToTable("tenant_role_permissions");

        builder.HasKey(x => x.Id).HasName("pk_tenant_role_permissions");

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

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.PermissionDefinitionId)
            .HasColumnName("permission_definition_id")
            .IsRequired();

        builder.Property(x => x.TenantRoleId)
            .HasColumnName("tenant_role_id")
            .IsRequired();

        builder.HasOne<TenantRole>()
            .WithMany()
            .HasForeignKey(x => x.TenantRoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_role_permissions_tenant_role_id_tenant_roles");

        builder.HasOne<PermissionDefinition>()
            .WithMany()
            .HasForeignKey(x => x.PermissionDefinitionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_role_permissions_permission_definition_id_permission_definitions");

        builder.HasIndex(x => new { x.TenantRoleId, x.PermissionDefinitionId })
            .IsUnique()
            .HasDatabaseName("uq_tenant_role_permissions_tenant_role_id_permission_definition_id");
    }
}

