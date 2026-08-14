using E_POS.Domain.Modules.ECommerce.Storefront.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantEntity = E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Configurations;

public sealed class StorefrontPolicyConfiguration : IEntityTypeConfiguration<StorefrontPolicy>
{
    public void Configure(EntityTypeBuilder<StorefrontPolicy> builder)
    {
        builder.ToTable("storefront_policies");

        builder.HasKey(x => x.Id).HasName("pk_storefront_policies");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.SalesChannelId).HasColumnName("sales_channel_id").IsRequired();
        builder.Property(x => x.PolicyType).HasColumnName("policy_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasColumnType("varchar(200)").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Content).HasColumnName("content").HasColumnType("text").IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnName("effective_from").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.PublishedAt).HasColumnName("published_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.HasOne<TenantEntity>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_storefront_policies_tenant_id_tenants");

        builder.HasOne<SalesChannel>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesChannelId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_storefront_policies_sales_channel_tenant");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_storefront_policies_created_by_tenant_user");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_storefront_policies_updated_by_tenant_user");

        builder.HasIndex(x => new { x.TenantId, x.SalesChannelId, x.PolicyType, x.Version })
            .IsUnique()
            .HasDatabaseName("uq_storefront_policies_type_version");

        builder.HasIndex(x => new { x.TenantId, x.SalesChannelId, x.PolicyType })
            .IsUnique()
            .HasDatabaseName("uq_storefront_policies_current_published")
            .HasFilter("status = 'PUBLISHED'");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_storefront_policies_policy_type", "policy_type IN ('TERMS', 'PRIVACY', 'CANCELLATION', 'COLLECTION', 'RETURN_REFUND')");
            t.HasCheckConstraint("ck_storefront_policies_status", "status IN ('DRAFT', 'PUBLISHED', 'ARCHIVED')");
            t.HasCheckConstraint("ck_storefront_policies_content_non_empty", "length(btrim(content)) > 0");
        });
    }
}
