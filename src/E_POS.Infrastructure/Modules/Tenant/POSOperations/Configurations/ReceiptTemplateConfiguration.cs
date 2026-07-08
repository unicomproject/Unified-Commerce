using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Configurations;

public sealed class ReceiptTemplateConfiguration : IEntityTypeConfiguration<ReceiptTemplate>
{
    public void Configure(EntityTypeBuilder<ReceiptTemplate> builder)
    {
        builder.ToTable("receipt_templates");

        builder.HasKey(x => x.Id).HasName("pk_receipt_templates");

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

        builder.Property(x => x.TemplateCode)
            .HasColumnName("template_code")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.TemplateName)
            .HasColumnName("template_name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TemplateType)
            .HasColumnName("template_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(x => x.IsBaseTemplate)
            .HasColumnName("is_base_template")
            .IsRequired();

        builder.Property(x => x.ParentTemplateId)
            .HasColumnName("parent_template_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_templates_tenant_id_tenants");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_templates_created_by_tenant_users");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_templates_updated_by_tenant_users");

        builder.HasOne<ReceiptTemplate>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ParentTemplateId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_templates_parent_template_id_receipt_templates");

        builder.HasIndex(x => new { x.TenantId, x.TemplateCode })
            .IsUnique()
            .HasDatabaseName("uq_receipt_templates_tenant_id_template_code");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_receipt_templates_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_receipt_templates_template_type", "template_type IN ('POS_RECEIPT', 'REFUND_RECEIPT', 'EXCHANGE_RECEIPT')");
            t.HasCheckConstraint("ck_receipt_templates_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        });
    }
}



