using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class TenantSubscriptionAddonConfiguration : IEntityTypeConfiguration<TenantSubscriptionAddon>
{
    public void Configure(EntityTypeBuilder<TenantSubscriptionAddon> builder)
    {
        builder.ToTable("tenant_subscription_addons");

        builder.HasKey(x => x.Id).HasName("pk_tenant_subscription_addons");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.SubscriptionAddonId)
            .HasColumnName("subscription_addon_id")
            .IsRequired();

        builder.Property(x => x.TenantSubscriptionId)
            .HasColumnName("tenant_subscription_id")
            .IsRequired();

        builder.Property(x => x.AddonId)
            .HasColumnName("addon_id")
            .IsRequired();

        builder.Property(x => x.SubscriptionId)
            .HasColumnName("subscription_id")
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasDefaultValue(1);

        builder.Property(x => x.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(18, 4);

        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3);

        builder.Property(x => x.AutoRenew)
            .HasColumnName("auto_renew")
            .HasDefaultValue(true);

        builder.Property(x => x.StartsAt)
            .HasColumnName("starts_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.EndsAt)
            .HasColumnName("ends_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id");

        builder.Property(x => x.UpdatedByPlatformUserId)
            .HasColumnName("updated_by_platform_user_id");

        builder.HasOne<TenantSubscription>()
            .WithMany()
            .HasForeignKey(x => x.TenantSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_subscription_addons_tenant_subscription_id_tenant_subscriptions");

        builder.HasOne<SubscriptionAddon>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionAddonId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_subscription_addons_subscription_addon_id_subscription_addons");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByPlatformUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tenant_subscription_addons_created_by_platform_user_id_platform_users");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByPlatformUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tenant_subscription_addons_updated_by_platform_user_id_platform_users");

        builder.HasIndex(x => new { x.TenantSubscriptionId, x.SubscriptionAddonId })
            .IsUnique()
            .HasDatabaseName("uq_tenant_subscription_addons_tenant_subscription_id_subscription_addon_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_tenant_subscription_addons_quantity", "quantity > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_tenant_subscription_addons_unit_price", "unit_price >= 0"));
    }
}



