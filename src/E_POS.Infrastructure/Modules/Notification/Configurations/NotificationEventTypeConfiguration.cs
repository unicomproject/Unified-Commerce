using E_POS.Domain.Modules.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Notification.Configurations;

public sealed class NotificationEventTypeConfiguration : IEntityTypeConfiguration<NotificationEventType>
{
    public void Configure(EntityTypeBuilder<NotificationEventType> builder)
    {
        builder.ToTable("notification_event_types");

        builder.HasKey(x => x.Id).HasName("pk_notification_event_types");

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
            .HasColumnName("tenant_id");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.DefaultPriority)
            .HasColumnName("default_priority")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.EventCode)
            .HasColumnName("event_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(x => new { x.TenantId, x.EventCode })
            .IsUnique()
            .HasDatabaseName("uq_notification_event_types_tenant_id_event_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_notification_event_types_default_priority", "default_priority IN ('LOW', 'NORMAL', 'HIGH', 'URGENT')")); 
    }
}

