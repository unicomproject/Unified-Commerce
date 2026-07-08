using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Configurations;

public sealed class TillConfiguration : IEntityTypeConfiguration<Till>
{
    public void Configure(EntityTypeBuilder<Till> builder)
    {
        builder.ToTable("tills");
        builder.HasKey(x => x.Id).HasName("pk_tills");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.TillCode).HasColumnName("till_code").HasColumnType("varchar(60)").HasMaxLength(60).IsRequired();
        builder.Property(x => x.TillName).HasColumnName("till_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.TillAreaName).HasColumnName("till_area_name").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.TillNumber).HasColumnName("till_number").IsRequired();
        builder.Property(x => x.TillType).HasColumnName("till_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.DefaultOpeningFloatAmount).HasColumnName("default_opening_float_amount").HasColumnType("numeric(18,4)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasColumnType("char(3)").HasMaxLength(3).IsRequired();
        builder.Property(x => x.IsCashManaged).HasColumnName("is_cash_managed").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id").IsRequired(false);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tills_tenant_id_tenants");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tills_outlet_id_outlets");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tills_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tills_updated_by_tenant_user_id_tenant_users");
        builder.HasIndex(x => new { x.TenantId, x.OutletId, x.TillCode }).IsUnique().HasDatabaseName("uq_tills_tenant_id_outlet_id_till_code");
        builder.HasIndex(x => new { x.TenantId, x.OutletId, x.TillAreaName, x.TillNumber }).IsUnique().HasDatabaseName("uq_tills_tenant_id_outlet_id_till_area_name_till_number");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_tills_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
            t.HasCheckConstraint("ck_tills_till_number_positive", "till_number > 0");
        });
    }
}
