using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Configurations;

public sealed class OutletConfiguration : IEntityTypeConfiguration<Outlet>
{
    public void Configure(EntityTypeBuilder<Outlet> builder)
    {
        builder.ToTable("outlets");
        builder.HasKey(x => x.Id).HasName("pk_outlets");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletCode).HasColumnName("outlet_code").HasColumnType("varchar(60)").HasMaxLength(60).IsRequired();
        builder.Property(x => x.OutletName).HasColumnName("outlet_name").HasColumnType("varchar(200)").HasMaxLength(200).IsRequired();
        builder.Property(x => x.OutletType).HasColumnName("outlet_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Phone).HasColumnName("phone").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired(false);
        builder.Property(x => x.Email).HasColumnName("email").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired(false);
        builder.Property(x => x.Timezone).HasColumnName("timezone").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.IsDefaultOutlet).HasColumnName("is_default_outlet").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id").IsRequired(false);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_outlets_tenant_id_tenants");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_outlets_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_outlets_updated_by_tenant_user_id_tenant_users");
        builder.HasIndex(x => new { x.TenantId, x.OutletCode }).IsUnique().HasDatabaseName("uq_outlets_tenant_id_outlet_code");
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("uq_outlets_tenant_id_default_outlet")
            .IsUnique()
            .HasFilter("is_default_outlet = true AND status <> 'DELETED'");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_outlets_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
            t.HasCheckConstraint("ck_outlets_outlet_type", "outlet_type IN ('STORE', 'WAREHOUSE')");
        });
    }
}
