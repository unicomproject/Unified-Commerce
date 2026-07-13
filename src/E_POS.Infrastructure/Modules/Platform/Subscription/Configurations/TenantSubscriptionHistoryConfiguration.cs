using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class TenantSubscriptionHistoryConfiguration : IEntityTypeConfiguration<TenantSubscriptionHistory>
{
    public void Configure(EntityTypeBuilder<TenantSubscriptionHistory> builder)
    {
        builder.ToTable("tenant_subscription_history");

        builder.HasKey(x => x.Id).HasName("pk_tenant_subscription_history");

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

        builder.Property(x => x.SequenceNumber)
            .HasColumnName("sequence_number");

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.TenantSubscriptionId)
            .HasColumnName("tenant_subscription_id")
            .IsRequired();

        builder.Property(x => x.SubscriptionId)
            .HasColumnName("subscription_id")
            .IsRequired();

        builder.Property(x => x.OldPlanId)
            .HasColumnName("old_plan_id");

        builder.Property(x => x.NewPlanId)
            .HasColumnName("new_plan_id");

        builder.Property(x => x.OldStatus)
            .HasColumnName("old_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.NewStatus)
            .HasColumnName("new_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.ChangeType)
            .HasColumnName("change_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasColumnType("text");

        builder.Property(x => x.ChangeData)
            .HasColumnName("change_data")
            .HasColumnType("jsonb");

        builder.Property(x => x.ChangedAt)
            .HasColumnName("changed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ChangedByPlatformUserId)
            .HasColumnName("changed_by_platform_user_id");

        builder.HasOne<TenantSubscription>()
            .WithMany()
            .HasForeignKey(x => x.TenantSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_subscription_history_tenant_subscription_id_tenant_subscriptions");

        builder.HasOne<TenantSubscription>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_subscription_history_subscription_id_tenant_subscriptions");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_subscription_history_tenant_id_tenants");

        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(x => x.OldPlanId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tenant_subscription_history_old_plan_id_subscription_plans");

        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(x => x.NewPlanId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tenant_subscription_history_new_plan_id_subscription_plans");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.ChangedByPlatformUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tenant_subscription_history_changed_by_platform_user_id_platform_users");

        builder.HasIndex(x => new { x.TenantSubscriptionId, x.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("uq_tenant_subscription_history_tenant_subscription_id_sequence_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_tenant_subscription_history_sequence_number", "sequence_number > 0")); 
    }
}



