using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ReturnPolicyTemplateConfiguration : IEntityTypeConfiguration<ReturnPolicyTemplate>
{
    public void Configure(EntityTypeBuilder<ReturnPolicyTemplate> builder)
    {
        builder.ToTable("return_policy_templates");

        builder.HasKey(x => x.Id).HasName("pk_return_policy_templates");

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

        builder.Property(x => x.TemplateCode)
            .HasColumnName("template_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.ReturnWindowDays)
            .HasColumnName("return_window_days");

        builder.Property(x => x.ExchangeWindowDays)
            .HasColumnName("exchange_window_days");

        builder.Property(x => x.RequiresReceipt)
            .HasColumnName("requires_receipt")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.AllowDefectiveReturn)
            .HasColumnName("allow_defective_return")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.RequiresManagerApproval)
            .HasColumnName("requires_manager_approval")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsPlatformDefault)
            .HasColumnName("is_platform_default")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.VersionNumber)
            .HasColumnName("version_number")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(x => x.LifecycleStatus)
            .HasColumnName("lifecycle_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasDefaultValue("DRAFT")
            .IsRequired();

        builder.Property(x => x.ConcurrencyToken)
            .HasColumnName("concurrency_token")
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(x => x.TemplateCode)
            .IsUnique()
            .HasDatabaseName("uq_return_policy_templates_template_code");

        builder.HasIndex(x => x.IsPlatformDefault)
            .IsUnique()
            .HasFilter("is_platform_default = true AND status = 'ACTIVE'")
            .HasDatabaseName("uq_return_policy_templates_platform_default");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_return_policy_templates_return_window_days", "return_window_days IS NULL OR return_window_days >= 0");
            t.HasCheckConstraint("ck_return_policy_templates_exchange_window_days", "exchange_window_days IS NULL OR exchange_window_days >= 0");
            t.HasCheckConstraint("ck_return_policy_templates_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
            t.HasCheckConstraint("ck_return_policy_templates_lifecycle_status", "lifecycle_status IN ('DRAFT', 'PUBLISHED', 'ARCHIVED')");
        });
    }
}

