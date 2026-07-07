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

        builder.Property(x => x.ReturnWindowDays)
            .HasColumnName("return_window_days");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(x => x.TemplateCode)
            .IsUnique()
            .HasDatabaseName("uq_return_policy_templates_template_code");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_return_policy_templates_return_window_days", "return_window_days IS NULL OR return_window_days >= 0");
            t.HasCheckConstraint("ck_return_policy_templates_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        });
    }
}

