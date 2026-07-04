using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.PricingTax.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.PricingTax.Configurations;

public sealed class PriceListChannelConfiguration : IEntityTypeConfiguration<PriceListChannel>
{
    public void Configure(EntityTypeBuilder<PriceListChannel> builder)
    {
        builder.ToTable("price_list_channels");

        builder.HasKey(x => x.Id).HasName("pk_price_list_channels");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.PriceListId).HasColumnName("price_list_id").IsRequired();
        builder.Property(x => x.SalesChannelId).HasColumnName("sales_channel_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_channels_tenant_id_tenants");
        builder.HasOne<PriceList>().WithMany().HasForeignKey(x => x.PriceListId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_channels_price_list_id_price_lists");
        builder.HasOne<SalesChannel>().WithMany().HasForeignKey(x => x.SalesChannelId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_channels_sales_channel_id_sales_channels");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_channels_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_list_channels_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.PriceListId, x.SalesChannelId }).IsUnique().HasDatabaseName("uq_price_list_channels_tenant_id_price_list_id_sales_channel_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_price_list_channels_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}
