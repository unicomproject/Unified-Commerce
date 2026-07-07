using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Configurations;

public sealed class PlatformPermissionConfiguration : IEntityTypeConfiguration<PlatformPermission>
{
    public void Configure(EntityTypeBuilder<PlatformPermission> builder)
    {
        builder.ToTable("platform_permissions");

        builder.HasKey(x => x.Id).HasName("pk_platform_permissions");

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

        builder.Property(x => x.PermissionCode)
            .HasColumnName("permission_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.HasIndex(x => x.PermissionCode)
            .IsUnique()
            .HasDatabaseName("uq_platform_permissions_permission_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_platform_permissions_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')")); 
    }
}



