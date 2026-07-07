using E_POS.Domain.Modules.Platform.Subscription.Entities;
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

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasDefaultValue(1);

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

        builder.HasIndex(x => new { x.TenantSubscriptionId, x.SubscriptionAddonId })
            .IsUnique()
            .HasDatabaseName("uq_tenant_subscription_addons_tenant_subscription_id_subscription_addon_id");
    }
}



