using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Configurations;

public sealed class SettingDefinitionConfiguration : IEntityTypeConfiguration<SettingDefinition>
{
    public void Configure(EntityTypeBuilder<SettingDefinition> builder)
    {
        builder.ToTable("setting_definitions");

        builder.HasKey(x => x.Id).HasName("pk_setting_definitions");

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

        builder.Property(x => x.SettingKey)
            .HasColumnName("setting_key")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired();
            
        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.ValueType)
            .HasColumnName("value_type")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();
            
        builder.Property(x => x.DefaultValue)
            .HasColumnName("default_value")
            .HasColumnType("jsonb")
            .IsRequired(false);
            
        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);
            
        builder.Property(x => x.IsTenantEditable)
            .HasColumnName("is_tenant_editable")
            .IsRequired();
            
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(x => x.SettingKey)
            .IsUnique()
            .HasDatabaseName("uq_setting_definitions_setting_key");
    }
}



