using E_POS.Domain.Modules.PlatformAdministration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.PlatformAdministration.Configurations;

public sealed class PlatformUserPermissionConfiguration : IEntityTypeConfiguration<PlatformUserPermission>
{
    public void Configure(EntityTypeBuilder<PlatformUserPermission> builder)
    {
        builder.ToTable("platform_user_permissions");

        builder.HasKey(x => x.Id).HasName("pk_platform_user_permissions");

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

        builder.Property(x => x.PlatformUserId)
            .HasColumnName("platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.PlatformPermissionId)
            .HasColumnName("platform_permission_id")
            .IsRequired();

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_user_permissions_platform_user_id_platform_users");

        builder.HasOne<PlatformPermission>()
            .WithMany()
            .HasForeignKey(x => x.PlatformPermissionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_user_permissions_platform_permission_id_platform_permissions");

        builder.HasIndex(x => new { x.PlatformUserId, x.PlatformPermissionId })
            .IsUnique()
            .HasDatabaseName("uq_platform_user_permissions_platform_user_id_platform_permission_id");
    }
}

