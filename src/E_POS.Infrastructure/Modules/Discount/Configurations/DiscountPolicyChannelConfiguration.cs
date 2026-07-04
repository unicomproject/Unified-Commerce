using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Discount.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Discount.Configurations;
public sealed class DiscountPolicyChannelConfiguration : IEntityTypeConfiguration<DiscountPolicyChannel>
{
    public void Configure(EntityTypeBuilder<DiscountPolicyChannel> builder)
    {
        builder.ToTable("discount_policy_channels");
        builder.HasKey(x => x.Id).HasName("pk_discount_policy_channels");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.DiscountPolicyId).HasColumnName("discount_policy_id").IsRequired();
        builder.Property(x => x.SalesChannelId).HasColumnName("sales_channel_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_channels_tenant_id_tenants");
        builder.HasOne<DiscountPolicy>().WithMany().HasForeignKey(x => x.DiscountPolicyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_channels_discount_policy_id_discount_policies");
        builder.HasOne<SalesChannel>().WithMany().HasForeignKey(x => x.SalesChannelId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_channels_sales_channel_id_sales_channels");
        builder.HasIndex(x => new { x.TenantId, x.DiscountPolicyId, x.SalesChannelId }).IsUnique().HasDatabaseName("uq_discount_policy_channels_tenant_id_discount_policy_id_sales_channel_id");
    }
}