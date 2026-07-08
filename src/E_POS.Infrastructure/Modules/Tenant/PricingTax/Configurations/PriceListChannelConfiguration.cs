using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.PricingTax.Configurations;

public sealed class PriceListChannelConfiguration : IEntityTypeConfiguration<PriceListChannel>
{
    public void Configure(EntityTypeBuilder<PriceListChannel> builder)
    {
        builder.ToTable("price_list_channels");

        builder.HasKey(x => x.Id).HasName("pk_price_list_channels");

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
        builder.Property(x => x.SalesChannelId).HasColumnName("sales_channel_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_channels_tenant_id_tenants");
        
        builder.HasOne<PriceList>().WithMany().HasForeignKey(x => new { x.TenantId, x.PriceListId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_channels_price_list_id_price_lists");
        builder.HasOne<SalesChannel>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesChannelId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_channels_sales_channel_id_sales_channels");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_channels_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_channels_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.PriceListId, x.SalesChannelId }).IsUnique().HasDatabaseName("uq_price_list_channels_tenant_id_price_list_id_sales_channel_id");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_price_list_channels_tenant_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_price_list_channels_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}



