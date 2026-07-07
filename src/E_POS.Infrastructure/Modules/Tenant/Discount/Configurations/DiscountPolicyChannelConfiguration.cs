using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Discount.Configurations;

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

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_channels_tenant_id_tenants");
        
        builder.HasOne<DiscountPolicy>().WithMany().HasForeignKey(x => new { x.TenantId, x.DiscountPolicyId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_channels_discount_policy_id_discount_policies");
        builder.HasOne<SalesChannel>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesChannelId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_channels_sales_channel_id_sales_channels");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_channels_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_discount_policy_channels_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.DiscountPolicyId, x.SalesChannelId }).IsUnique().HasDatabaseName("uq_discount_policy_channels_tenant_id_discount_policy_id_sales_channel_id");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_discount_policy_channels_tenant_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_discount_policy_channels_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}


