using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Configurations;

public sealed class OutletAddressConfiguration : IEntityTypeConfiguration<OutletAddress>
{
    public void Configure(EntityTypeBuilder<OutletAddress> builder)
    {
        builder.ToTable("outlet_addresses");
        builder.HasKey(x => x.Id).HasName("pk_outlet_addresses");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.AddressType).HasColumnName("address_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.AddressLine1).HasColumnName("address_line1").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(x => x.AddressLine2).HasColumnName("address_line2").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired(false);
        builder.Property(x => x.City).HasColumnName("city").HasColumnType("varchar(120)").HasMaxLength(120).IsRequired();
        builder.Property(x => x.StateOrProvince).HasColumnName("state_or_province").HasColumnType("varchar(120)").HasMaxLength(120).IsRequired(false);
        builder.Property(x => x.PostalCode).HasColumnName("postal_code").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired(false);
        builder.Property(x => x.CountryCode).HasColumnName("country_code").HasColumnType("char(2)").HasMaxLength(2).IsRequired();
        builder.Property(x => x.ContactName).HasColumnName("contact_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired(false);
        builder.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired(false);
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id").IsRequired(false);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_outlet_addresses_tenant_id_tenants");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_outlet_addresses_outlet_id_outlets");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_outlet_addresses_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_outlet_addresses_updated_by_tenant_user_id_tenant_users");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_outlet_addresses_address_type", "address_type IN ('PHYSICAL', 'BILLING', 'PICKUP')");
            t.HasCheckConstraint("ck_outlet_addresses_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        });
    }
}
