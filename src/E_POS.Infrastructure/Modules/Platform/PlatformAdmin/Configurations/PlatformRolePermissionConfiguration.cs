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

        builder.HasIndex(x => new { x.PlatformRoleId, x.PlatformPermissionId })
            .IsUnique()
            .HasDatabaseName("uq_platform_role_permissions_platform_role_id_platform_permission_id");
    }
}



