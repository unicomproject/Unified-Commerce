using E_POS.Domain.Modules.SubscriptionBilling.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.SubscriptionBilling.Configurations;

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

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_subscriptions_tenant_id_tenants");

        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_subscriptions_subscription_plan_id_subscription_plans");

        builder.HasIndex(x => new { x.TenantId, x.SubscriptionNumber })
            .IsUnique()
            .HasDatabaseName("uq_tenant_subscriptions_tenant_id_subscription_number");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_tenant_subscriptions_subscription_status",
            "subscription_status IN ('TRIAL', 'ACTIVE', 'PAST_DUE', 'CANCELLED', 'EXPIRED')"));
    }
}
