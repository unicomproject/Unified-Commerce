using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Configurations;

public sealed class PlatformRolePermissionConfiguration : IEntityTypeConfiguration<PlatformRolePermission>
{
    public void Configure(EntityTypeBuilder<PlatformRolePermission> builder)
    {
        builder.ToTable("platform_role_permissions");

        builder.HasKey(x => x.Id).HasName("pk_platform_role_permissions");

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

        builder.Property(x => x.PlatformPermissionId)
            .HasColumnName("platform_permission_id")
            .IsRequired();

        builder.Property(x => x.PlatformRoleId)
            .HasColumnName("platform_role_id")
            .IsRequired();

        builder.Property(x => x.GrantedAt)
            .HasColumnName("granted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.GrantedByPlatformUserId)
            .HasColumnName("granted_by_platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.RevokedByPlatformUserId)
            .HasColumnName("revoked_by_platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.RevokedReason)
            .HasColumnName("revoked_reason")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250)
            .IsRequired(false);

        builder.HasOne<PlatformRole>()
            .WithMany()
            .HasForeignKey(x => x.PlatformRoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_role_permissions_platform_role_id_platform_roles");

        builder.HasOne<PlatformPermission>()
            .WithMany()
            .HasForeignKey(x => x.PlatformPermissionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_role_permissions_platform_permission_id_platform_permissions");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.GrantedByPlatformUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_platform_role_permissions_granted_by_platform_user_id_platform_users");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.RevokedByPlatformUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_platform_role_permissions_revoked_by_platform_user_id_platform_users");

        builder.HasIndex(x => new { x.PlatformRoleId, x.PlatformPermissionId })
            .IsUnique()
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("uq_platform_role_permissions_platform_role_id_platform_permission_id");
    }
}
