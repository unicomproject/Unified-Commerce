using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        builder.ToTable("tenant_subscriptions");

        builder.HasKey(x => x.Id).HasName("pk_tenant_subscriptions");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.SubscriptionNumber).HasColumnName("subscription_number").HasColumnType("varchar(80)").HasMaxLength(80);
        builder.Property(x => x.SubscriptionPlanId).HasColumnName("subscription_plan_id").IsRequired();
        builder.Property(x => x.SubscriptionStatus).HasColumnName("subscription_status").HasColumnType("varchar(30)").HasMaxLength(30);
        builder.Property(x => x.PlanId).HasColumnName("plan_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(40)").HasMaxLength(40);
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasColumnType("char(3)").HasMaxLength(3);
        builder.Property(x => x.PlanPrice).HasColumnName("plan_price").HasPrecision(18, 4);
        builder.Property(x => x.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CurrentPeriodStart).HasColumnName("current_period_start").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CurrentPeriodEnd).HasColumnName("current_period_end").HasColumnType("timestamp with time zone");
        builder.Property(x => x.NextBillingDate).HasColumnName("next_billing_date").HasColumnType("timestamp with time zone");
        builder.Property(x => x.TrialStartedAt).HasColumnName("trial_started_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.TrialEndsAt).HasColumnName("trial_ends_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.EndedAt).HasColumnName("ended_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasColumnType("text");
        builder.Property(x => x.AssignedByPlatformUserId).HasColumnName("assigned_by_platform_user_id");
        builder.Property(x => x.BillingCycle).HasColumnName("billing_cycle").HasColumnType("varchar(20)").HasMaxLength(20).HasDefaultValue("monthly");
        builder.Property(x => x.TrialStartAt).HasColumnName("trial_start_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.TrialEndAt).HasColumnName("trial_end_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.BillingStartAt).HasColumnName("billing_start_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.NextBillingAt).HasColumnName("next_billing_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.AutoRenew).HasColumnName("auto_renew").HasDefaultValue(true);
        builder.Property(x => x.DiscountType).HasColumnName("discount_type").HasColumnType("varchar(20)").HasMaxLength(20);
        builder.Property(x => x.DiscountValue).HasColumnName("discount_value").HasPrecision(18, 2);
        builder.Property(x => x.TaxPercentage).HasColumnName("tax_percentage").HasPrecision(8, 2).HasDefaultValue(0m);
        builder.Property(x => x.InvoiceEmail).HasColumnName("invoice_email").HasColumnType("varchar(255)").HasMaxLength(255);
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasColumnType("varchar(80)").HasMaxLength(80);
        builder.Property(x => x.Notes).HasColumnName("notes").HasColumnType("text");
        builder.Property(x => x.MaxOutletsOverride).HasColumnName("max_outlets_override");
        builder.Property(x => x.MaxTillsOverride).HasColumnName("max_tills_override");
        builder.Property(x => x.MaxUsersOverride).HasColumnName("max_users_override");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_subscriptions_tenant_id_tenants");

        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_subscriptions_subscription_plan_id_subscription_plans");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.AssignedByPlatformUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tenant_subscriptions_assigned_by_platform_user_id_platform_users");

        builder.HasIndex(x => new { x.TenantId, x.SubscriptionNumber })
            .IsUnique()
            .HasDatabaseName("uq_tenant_subscriptions_tenant_id_subscription_number");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("ix_tenant_subscriptions_tenant_id_status");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_tenant_subscriptions_subscription_status",
            "subscription_status IN ('TRIAL', 'ACTIVE', 'PAST_DUE', 'CANCELLED', 'EXPIRED')"));
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_tenant_subscriptions_plan_price",
            "plan_price >= 0"));
    }
}



