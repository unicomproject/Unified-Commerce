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

        builder.Ignore(x => x.UpdatedAt);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired(false);

        builder.Property(x => x.NotificationTemplateId)
            .HasColumnName("notification_template_id")
            .IsRequired();

        builder.Property(x => x.VersionNumber)
            .HasColumnName("version_number")
            .IsRequired();

        builder.Property(x => x.SubjectTemplate)
            .HasColumnName("subject_template")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(x => x.TitleTemplate)
            .HasColumnName("title_template")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(x => x.BodyTextTemplate)
            .HasColumnName("body_text_template")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.BodyHtmlTemplate)
            .HasColumnName("body_html_template")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.ActionUrlTemplate)
            .HasColumnName("action_url_template")
            .HasColumnType("varchar(700)")
            .HasMaxLength(700)
            .IsRequired(false);

        builder.Property(x => x.ActionLabelTemplate)
            .HasColumnName("action_label_template")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(x => x.VariablesSchemaJson)
            .HasColumnName("variables_schema_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.SamplePayloadJson)
            .HasColumnName("sample_payload_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.IsActiveVersion)
            .HasColumnName("is_active_version")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.NotificationTemplateId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("ux_notification_template_versions_d19e2b03");

        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_template_versions_055f2a22");

        builder.HasOne<E_POS.Domain.Modules.Notification.Entities.NotificationTemplate>()
            .WithMany()
            .HasForeignKey(x => x.NotificationTemplateId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_template_versions_80211f79");

        builder.HasOne<E_POS.Domain.Modules.PlatformAdministration.Entities.PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByPlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_template_versions_0d2d30cd");

        builder.HasOne<E_POS.Domain.Modules.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_notification_template_versions_3d56cee8");
        // </second-brain-constraints>
    }
}