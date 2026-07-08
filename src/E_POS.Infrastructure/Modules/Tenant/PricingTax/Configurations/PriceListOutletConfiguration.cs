using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.PricingTax.Configurations;

public sealed class PriceListOutletConfiguration : IEntityTypeConfiguration<PriceListOutlet>
{
    public void Configure(EntityTypeBuilder<PriceListOutlet> builder)
    {
        builder.ToTable("price_list_outlets");

        builder.HasKey(x => x.Id).HasName("pk_price_list_outlets");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.PriceListId).HasColumnName("price_list_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_outlets_tenant_id_tenants");
        
        builder.HasOne<PriceList>().WithMany().HasForeignKey(x => new { x.TenantId, x.PriceListId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_outlets_price_list_id_price_lists");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => new { x.TenantId, x.OutletId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_outlets_outlet_id_outlets");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_outlets_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_outlets_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.PriceListId, x.OutletId }).IsUnique().HasDatabaseName("uq_price_list_outlets_tenant_id_price_list_id_outlet_id");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_price_list_outlets_tenant_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_price_list_outlets_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}



