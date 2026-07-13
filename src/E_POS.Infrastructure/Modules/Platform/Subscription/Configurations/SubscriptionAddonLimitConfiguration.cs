using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionAddonLimitConfiguration : IEntityTypeConfiguration<SubscriptionAddonLimit>
{
    public void Configure(EntityTypeBuilder<SubscriptionAddonLimit> builder)
    {
        builder.ToTable("subscription_addon_limits");

        builder.HasKey(x => x.Id).HasName("pk_subscription_addon_limits");

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

        builder.Property(x => x.SubscriptionAddonId)
            .HasColumnName("subscription_addon_id")
            .IsRequired();

        builder.Property(x => x.FeatureLimitDefinitionId)
            .HasColumnName("feature_limit_definition_id")
            .IsRequired();

        builder.Property(x => x.IncrementValue)
            .HasColumnName("increment_value")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.HasOne<SubscriptionAddon>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionAddonId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_addon_limits_subscription_addon_id_subscription_addons");

        builder.HasOne<FeatureLimitDefinition>()
            .WithMany()
            .HasForeignKey(x => x.FeatureLimitDefinitionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_addon_limits_feature_limit_definition_id_feature_limit_definitions");

        builder.HasIndex(x => new { x.SubscriptionAddonId, x.FeatureLimitDefinitionId })
            .IsUnique()
            .HasDatabaseName("uq_subscription_addon_limits_subscription_addon_id_feature_limit_definition_id");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_subscription_addon_limits_increment_value",
            "increment_value > 0"));
    }
}
