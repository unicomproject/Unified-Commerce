using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>
{
    public void Configure(EntityTypeBuilder<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(x => x.Id).HasName("pk_tenants");

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

        builder.Property(x => x.TenantCode)
            .HasColumnName("tenant_code")
            .HasColumnType("varchar(60)")
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.BaseCurrency)
            .HasColumnName("base_currency")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.BillingStatus)
            .HasColumnName("billing_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.BusinessType)
            .HasColumnName("business_type")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired(false);

        builder.Property(x => x.BusinessTypeId)
            .HasColumnName("business_type_id")
            .IsRequired();

        builder.Property(x => x.DefaultLocale)
            .HasColumnName("default_locale")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.DefaultTimezone)
            .HasColumnName("default_timezone")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.OperatingMode)
            .HasColumnName("operating_mode")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.PrimaryDomain)
            .HasColumnName("primary_domain")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.HasOne<BusinessType>()
            .WithMany()
            .HasForeignKey(x => x.BusinessTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenants_business_type_id_business_types");

        builder.HasIndex(x => x.TenantCode)
            .IsUnique()
            .HasDatabaseName("uq_tenants_tenant_code");

        builder.HasIndex(x => x.PrimaryDomain)
            .IsUnique()
            .HasDatabaseName("uq_tenants_primary_domain");
    }
}



