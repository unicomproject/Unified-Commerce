using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Discount.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Discount.Configurations;
public sealed class ExpiryDiscountRuleTierConfiguration : IEntityTypeConfiguration<ExpiryDiscountRuleTier>
{
    public void Configure(EntityTypeBuilder<ExpiryDiscountRuleTier> builder)
    {
        builder.ToTable("expiry_discount_rule_tiers");
        builder.HasKey(x => x.Id).HasName("pk_expiry_discount_rule_tiers");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ExpiryDiscountRuleId).HasColumnName("expiry_discount_rule_id").IsRequired();
        builder.Property(x => x.TierName).HasColumnName("tier_name").HasColumnType("varchar(120)").HasMaxLength(120).IsRequired(false);
        builder.Property(x => x.StartsDaysBeforeExpiry).HasColumnName("starts_days_before_expiry").IsRequired();
        builder.Property(x => x.EndsDaysBeforeExpiry).HasColumnName("ends_days_before_expiry").IsRequired();
        builder.Property(x => x.DiscountPercent).HasColumnName("discount_percent").HasPrecision(8, 4).IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_expiry_discount_rule_tiers_tenant_id_tenants");
        builder.HasOne<ExpiryDiscountRule>().WithMany().HasForeignKey(x => x.ExpiryDiscountRuleId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_expiry_discount_rule_tiers_expiry_discount_rule_id_expiry_discount_rules");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_expiry_discount_rule_tiers_days", "starts_days_before_expiry >= 0 AND ends_days_before_expiry >= 0 AND starts_days_before_expiry >= ends_days_before_expiry");
            t.HasCheckConstraint("ck_expiry_discount_rule_tiers_discount_percent", "discount_percent >= 0 AND discount_percent <= 100");
            t.HasCheckConstraint("ck_expiry_discount_rule_tiers_sort_order", "sort_order >= 0");
        });
    }
}