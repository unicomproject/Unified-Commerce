using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.AccessControl.Configurations;

public sealed class OutletUserPermissionConfiguration : IEntityTypeConfiguration<OutletUserPermission>
{
    public void Configure(EntityTypeBuilder<OutletUserPermission> builder)
    {
        builder.ToTable("outlet_user_permissions");

        builder.HasKey(x => x.Id).HasName("pk_outlet_user_permissions");

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

        builder.Property(x => x.TenantUserId)
            .HasColumnName("tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id")
            .IsRequired(false);

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.PermissionDefinitionId)
            .HasColumnName("permission_definition_id")
            .IsRequired();

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => x.OutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_outlet_user_permissions_outlet_id_outlets");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.TenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_outlet_user_permissions_tenant_user_id_tenant_users");

        builder.HasOne<PermissionDefinition>()
            .WithMany()
            .HasForeignKey(x => x.PermissionDefinitionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_outlet_user_permissions_permission_definition_id_permission_definitions");

        builder.HasIndex(x => new { x.OutletId, x.TenantUserId, x.PermissionDefinitionId })
            .IsUnique()
            .HasDatabaseName("uq_outlet_user_permissions_outlet_id_tenant_user_id_permission_definition_id");
    }
}

