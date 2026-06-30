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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.SubscriptionNumber)
            .HasColumnName("subscription_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.SubscriptionPlanId)
            .HasColumnName("subscription_plan_id")
            .IsRequired();

        builder.Property(x => x.SubscriptionStatus)
            .HasColumnName("subscription_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

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

        builder.ToTable(t => t.HasCheckConstraint("ck_tenant_subscriptions_subscription_status", "subscription_status IN ('TRIAL', 'ACTIVE', 'PAST_DUE', 'CANCELLED', 'EXPIRED')")); 
    }
}

