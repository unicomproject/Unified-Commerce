using E_POS.Domain.Modules.SubscriptionBilling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.SubscriptionBilling.Configurations;

public sealed class SubscriptionAddonFeatureConfiguration : IEntityTypeConfiguration<SubscriptionAddonFeature>
{
    public void Configure(EntityTypeBuilder<SubscriptionAddonFeature> builder)
    {
        builder.ToTable("subscription_addon_features");

        builder.HasKey(x => x.Id).HasName("pk_subscription_addon_features");

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

        builder.Property(x => x.PlatformFeatureId)
            .HasColumnName("platform_feature_id")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.SubscriptionAddonId)
            .HasColumnName("subscription_addon_id")
            .IsRequired();

        builder.HasOne<SubscriptionAddon>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionAddonId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_addon_features_subscription_addon_id_subscription_addons");

        builder.HasOne<PlatformFeature>()
            .WithMany()
            .HasForeignKey(x => x.PlatformFeatureId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_addon_features_platform_feature_id_platform_features");

        builder.HasIndex(x => new { x.SubscriptionAddonId, x.PlatformFeatureId })
            .IsUnique()
            .HasDatabaseName("uq_subscription_addon_features_subscription_addon_id_platform_feature_id");
    }
}

