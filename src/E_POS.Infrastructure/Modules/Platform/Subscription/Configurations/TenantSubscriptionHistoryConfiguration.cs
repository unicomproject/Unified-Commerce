using E_POS.Domain.Modules.Platform.Subscription.Entities;
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

        builder.Property(x => x.TenantSubscriptionId)
            .HasColumnName("tenant_subscription_id")
            .IsRequired();

        builder.HasOne<TenantSubscription>()
            .WithMany()
            .HasForeignKey(x => x.TenantSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_subscription_history_tenant_subscription_id_tenant_subscriptions");

        builder.HasIndex(x => new { x.TenantSubscriptionId, x.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("uq_tenant_subscription_history_tenant_subscription_id_sequence_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_tenant_subscription_history_sequence_number", "sequence_number > 0")); 
    }
}



