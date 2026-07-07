using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Configurations;

public sealed class OutletUserRoleConfiguration : IEntityTypeConfiguration<OutletUserRole>
{
    public void Configure(EntityTypeBuilder<OutletUserRole> builder)
    {
        builder.ToTable("outlet_user_roles");

        builder.HasKey(x => x.Id).HasName("pk_outlet_user_roles");

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

        builder.Property(x => x.TenantRoleId)
            .HasColumnName("tenant_role_id")
            .IsRequired();

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => x.OutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_outlet_user_roles_outlet_id_outlets");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.TenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_outlet_user_roles_tenant_user_id_tenant_users");

        builder.HasOne<TenantRole>()
            .WithMany()
            .HasForeignKey(x => x.TenantRoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_outlet_user_roles_tenant_role_id_tenant_roles");

        builder.HasIndex(x => new { x.OutletId, x.TenantUserId, x.TenantRoleId })
            .IsUnique()
            .HasDatabaseName("uq_outlet_user_roles_outlet_id_tenant_user_id_tenant_role_id");
    }
}



