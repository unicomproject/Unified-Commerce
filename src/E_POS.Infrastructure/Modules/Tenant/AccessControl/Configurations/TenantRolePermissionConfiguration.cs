using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Configurations;

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

        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.TenantRoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(x => x.PermissionDefinitionId)
            .HasColumnName("permission_id")
            .IsRequired();

        builder.Property(x => x.GrantedByTenantUserId)
            .HasColumnName("granted_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.RevokedByTenantUserId)
            .HasColumnName("revoked_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.GrantedAt)
            .HasColumnName("granted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasColumnType("text")
            .IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_role_permissions_tenant_id_tenants");

        builder.HasOne<TenantRole>()
            .WithMany()
            .HasForeignKey(x => x.TenantRoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_role_permissions_role_id_tenant_roles");

        builder.HasOne<PermissionDefinition>()
            .WithMany()
            .HasForeignKey(x => x.PermissionDefinitionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_role_permissions_permission_id_permission_definitions");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.GrantedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_role_permissions_granted_by");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.RevokedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_role_permissions_revoked_by");

        builder.HasIndex(x => new { x.TenantId, x.TenantRoleId, x.PermissionDefinitionId })
            .IsUnique()
            .HasDatabaseName("uq_tenant_role_permissions_tenant_id_role_id_permission_id");
    }
}
