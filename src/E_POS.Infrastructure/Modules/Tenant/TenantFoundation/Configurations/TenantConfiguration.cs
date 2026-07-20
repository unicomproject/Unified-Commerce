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

        builder.Property(x => x.TenantSlug)
            .HasColumnName("tenant_slug")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.BaseCurrencyCode)
            .HasColumnName("base_currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.DefaultTimezone)
            .HasColumnName("default_timezone")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.DefaultLocale)
            .HasColumnName("default_locale")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(x => x.OperatingMode)
            .HasColumnName("operating_mode")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired(false);

        builder.Property(x => x.DataRegion)
            .HasColumnName("data_region")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.ActivatedAt)
            .HasColumnName("activated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.SuspendedAt)
            .HasColumnName("suspended_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ArchivedAt)
            .HasColumnName("archived_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByPlatformUserId)
            .HasColumnName("updated_by_platform_user_id")
            .IsRequired(false);

        builder.HasOne<Currency>()
            .WithMany()
            .HasForeignKey(x => x.BaseCurrencyCode)
            .HasPrincipalKey(x => x.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenants_base_currency_code_currencies");

        builder.HasIndex(x => x.TenantCode)
            .IsUnique()
            .HasDatabaseName("uq_tenants_tenant_code");

        builder.HasIndex(x => x.TenantSlug)
            .IsUnique()
            .HasDatabaseName("uq_tenants_tenant_slug");
    }
}
