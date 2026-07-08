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

        builder.Property(x => x.FeatureLimitDefinitionId)
            .HasColumnName("feature_limit_definition_id")
            .IsRequired();

        builder.Property(x => x.LimitValue)
            .HasColumnName("limit_value");

        builder.Property(x => x.SubscriptionAddonFeatureId)
            .HasColumnName("subscription_addon_feature_id")
            .IsRequired();

        builder.HasOne<SubscriptionAddonFeature>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionAddonFeatureId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_addon_limits_subscription_addon_feature_id_subscription_addon_features");

        builder.HasOne<FeatureLimitDefinition>()
            .WithMany()
            .HasForeignKey(x => x.FeatureLimitDefinitionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_addon_limits_feature_limit_definition_id_feature_limit_definitions");

        builder.HasIndex(x => new { x.SubscriptionAddonFeatureId, x.FeatureLimitDefinitionId })
            .IsUnique()
            .HasDatabaseName("uq_subscription_addon_limits_subscription_addon_feature_id_feature_limit_definition_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_addon_limits_limit_value", "limit_value IS NULL OR limit_value >= 0")); 
    }
}



