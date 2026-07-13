using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");

        builder.HasKey(x => x.Id).HasName("pk_subscription_plans");

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

        builder.Property(x => x.PlanCode)
            .HasColumnName("plan_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.BillingInterval)
            .HasColumnName("billing_interval")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.BaseCurrency)
            .HasColumnName("base_currency")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.PriceAmount)
            .HasColumnName("price_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.MaxOutlets)
            .HasColumnName("max_outlets")
            .IsRequired(false);

        builder.Property(x => x.MaxUsers)
            .HasColumnName("max_users")
            .IsRequired(false);

        builder.Property(x => x.MaxTills)
            .HasColumnName("max_tills")
            .IsRequired(false);

        builder.Property(x => x.PlanName)
            .HasColumnName("plan_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.BillingCycle)
            .HasColumnName("billing_cycle")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.BaseCurrencyCode)
            .HasColumnName("base_currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.BasePrice)
            .HasColumnName("base_price")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.TrialDays)
            .HasColumnName("trial_days")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.IsPublic)
            .HasColumnName("is_public")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.IsCustomPlan)
            .HasColumnName("is_custom_plan")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByPlatformUserId)
            .HasColumnName("updated_by_platform_user_id")
            .IsRequired(false);

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByPlatformUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_subscription_plans_created_by_platform_user_id_platform_users");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByPlatformUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_subscription_plans_updated_by_platform_user_id_platform_users");

        builder.HasIndex(x => x.PlanCode)
            .IsUnique()
            .HasDatabaseName("uq_subscription_plans_plan_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_plans_billing_interval", "billing_interval IN ('MONTHLY', 'YEARLY', 'ONE_TIME')"));
        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_plans_price_amount", "price_amount >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_plans_base_price", "base_price >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_plans_trial_days", "trial_days >= 0"));
    }
}
