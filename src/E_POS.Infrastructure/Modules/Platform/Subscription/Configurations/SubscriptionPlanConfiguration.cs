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

        builder.HasIndex(x => x.PlanCode)
            .IsUnique()
            .HasDatabaseName("uq_subscription_plans_plan_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_plans_billing_interval", "billing_interval IN ('MONTHLY', 'YEARLY', 'ONE_TIME')")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_plans_price_amount", "price_amount >= 0")); 
    }
}



