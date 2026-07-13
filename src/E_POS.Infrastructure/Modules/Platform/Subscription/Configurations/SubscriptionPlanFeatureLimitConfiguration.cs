using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionPlanFeatureLimitConfiguration : IEntityTypeConfiguration<SubscriptionPlanFeatureLimit>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlanFeatureLimit> builder)
    {
        builder.ToTable("subscription_plan_feature_limits");

        builder.HasKey(x => x.Id).HasName("pk_subscription_plan_feature_limits");

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

        builder.Property(x => x.SubscriptionPlanId)
            .HasColumnName("subscription_plan_id")
            .IsRequired();

        builder.Property(x => x.FeatureLimitDefinitionId)
            .HasColumnName("feature_limit_definition_id")
            .IsRequired();

        builder.Property(x => x.LimitValue)
            .HasColumnName("limit_value")
            .HasPrecision(18, 4);

        builder.Property(x => x.IsUnlimited)
            .HasColumnName("is_unlimited")
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_plan_feature_limits_subscription_plan_id_subscription_plans");

        builder.HasOne<FeatureLimitDefinition>()
            .WithMany()
            .HasForeignKey(x => x.FeatureLimitDefinitionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_plan_feature_limits_feature_limit_definition_id_feature_limit_definitions");

        builder.HasIndex(x => new { x.SubscriptionPlanId, x.FeatureLimitDefinitionId })
            .IsUnique()
            .HasDatabaseName("uq_subscription_plan_feature_limits_subscription_plan_id_feature_limit_definition_id");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_subscription_plan_feature_limits_limit_value",
            "limit_value IS NULL OR limit_value >= 0"));
    }
}
