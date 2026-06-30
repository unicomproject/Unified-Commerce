using E_POS.Domain.Modules.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Notification.Configurations;

public sealed class NotificationTemplateVersionConfiguration : IEntityTypeConfiguration<NotificationTemplateVersion>
{
    public void Configure(EntityTypeBuilder<NotificationTemplateVersion> builder)
    {
        builder.ToTable("notification_template_versions");

        builder.HasKey(x => x.Id).HasName("pk_notification_template_versions");

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

        builder.Property(x => x.NotificationTemplateId)
            .HasColumnName("notification_template_id")
            .IsRequired();

        builder.Property(x => x.IsActiveVersion)
            .HasColumnName("is_active_version")
            .IsRequired();
        builder.Property(x => x.VersionNumber)
            .HasColumnName("version_number");

        builder.HasOne<NotificationTemplate>()
            .WithMany()
            .HasForeignKey(x => x.NotificationTemplateId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_template_versions_notification_template_id_notification_templates");

        builder.HasIndex(x => new { x.NotificationTemplateId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("uq_notification_template_versions_notification_template_id_version_number");

        builder.HasIndex(x => x.NotificationTemplateId)
            .IsUnique()
            .HasDatabaseName("uq_notification_template_versions_notification_template_id")
            .HasFilter("is_active_version = true");

        builder.ToTable(t => t.HasCheckConstraint("ck_notification_template_versions_version_number", "version_number > 0")); 
    }
}

