using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Configurations;

public sealed class ReceiptTemplateVersionConfiguration : IEntityTypeConfiguration<ReceiptTemplateVersion>
{
    public void Configure(EntityTypeBuilder<ReceiptTemplateVersion> builder)
    {
        builder.ToTable("receipt_template_versions");

        builder.HasKey(x => x.Id).HasName("pk_receipt_template_versions");

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

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by_tenant_user_id");

        builder.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by_tenant_user_id");

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.ReceiptTemplateId)
            .HasColumnName("receipt_template_id")
            .IsRequired();

        builder.Property(x => x.VersionNumber)
            .HasColumnName("version_number")
            .IsRequired();

        builder.Property(x => x.TemplateData)
            .HasColumnName("template_data")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.PageSize)
            .HasColumnName("page_size")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20);

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.EffectiveTo)
            .HasColumnName("effective_to")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_template_versions_tenant_id_tenants");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_template_versions_created_by_tenant_users");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_template_versions_updated_by_tenant_users");

        builder.HasOne<ReceiptTemplate>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReceiptTemplateId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_template_versions_receipt_template_id_receipt_templates");

        builder.HasIndex(x => new { x.TenantId, x.ReceiptTemplateId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("uq_receipt_template_versions_receipt_template_id_version_number");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_receipt_template_versions_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_receipt_template_versions_version_number", "version_number > 0");
            t.HasCheckConstraint("ck_receipt_template_versions_effective_dates", "effective_to IS NULL OR effective_from IS NULL OR effective_to >= effective_from");
        });
    }
}



