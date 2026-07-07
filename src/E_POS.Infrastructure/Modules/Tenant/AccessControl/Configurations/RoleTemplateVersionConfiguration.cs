using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Configurations;

public sealed class RoleTemplateVersionConfiguration : IEntityTypeConfiguration<RoleTemplateVersion>
{
    public void Configure(EntityTypeBuilder<RoleTemplateVersion> builder)
    {
        builder.ToTable("role_template_versions");

        builder.HasKey(x => x.Id).HasName("pk_role_template_versions");

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

        builder.Property(x => x.RoleTemplateId)
            .HasColumnName("role_template_id")
            .IsRequired();

        builder.Property(x => x.VersionNumber)
            .HasColumnName("version_number")
            .IsRequired();

        builder.Property(x => x.VersionLabel)
            .HasColumnName("version_label")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.EffectiveUntil)
            .HasColumnName("effective_until")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.HasOne<RoleTemplate>()
            .WithMany()
            .HasForeignKey(x => x.RoleTemplateId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_role_template_versions_role_template_id_role_templates");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_role_template_versions_created_by");

        builder.HasIndex(x => new { x.RoleTemplateId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("uq_role_template_versions_role_template_id_version_number");

        builder.ToTable(t => 
        {
            t.HasCheckConstraint("ck_role_template_versions_version_number", "version_number > 0");
            t.HasCheckConstraint("ck_role_template_versions_effective_dates", "effective_until IS NULL OR effective_until > effective_from");
        });
    }
}
