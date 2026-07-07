using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionPlanAddonConfiguration : IEntityTypeConfiguration<SubscriptionPlanAddon>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlanAddon> builder)
    {
        builder.ToTable("subscription_plan_addons");

        builder.HasKey(x => x.Id).HasName("pk_subscription_plan_addons");

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

        builder.Property(x => x.SubscriptionPlanId)
            .HasColumnName("subscription_plan_id")
            .IsRequired();

        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_plan_addons_subscription_plan_id_subscription_plans");

        builder.HasOne<SubscriptionAddon>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionAddonId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_plan_addons_subscription_addon_id_subscription_addons");

        builder.HasIndex(x => new { x.SubscriptionPlanId, x.SubscriptionAddonId })
            .IsUnique()
            .HasDatabaseName("uq_subscription_plan_addons_subscription_plan_id_subscription_addon_id");
    }
}



