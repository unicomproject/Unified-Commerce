using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Configurations;

public sealed class TenantUserRoleConfiguration : IEntityTypeConfiguration<TenantUserRole>
{
    public void Configure(EntityTypeBuilder<TenantUserRole> builder)
    {
        builder.ToTable("tenant_user_roles");

        builder.HasKey(x => x.Id).HasName("pk_tenant_user_roles");

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

        builder.Property(x => x.TenantUserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.TenantRoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(x => x.AssignedByTenantUserId)
            .HasColumnName("assigned_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.AssignedAt)
            .HasColumnName("assigned_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_user_roles_tenant_id_tenants");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.TenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_user_roles_user_id_tenant_users");

        builder.HasOne<TenantRole>()
            .WithMany()
            .HasForeignKey(x => x.TenantRoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_user_roles_role_id_tenant_roles");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.AssignedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_user_roles_assigned_by");

        builder.HasIndex(x => new { x.TenantId, x.TenantUserId, x.TenantRoleId })
            .IsUnique()
            .HasDatabaseName("uq_tenant_user_roles_tenant_id_user_id_role_id");
    }
}
