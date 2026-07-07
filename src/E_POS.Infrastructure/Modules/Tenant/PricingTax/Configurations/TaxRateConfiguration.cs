using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.PricingTax.Configurations;

public sealed class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> builder)
    {
        builder.ToTable("tax_rates");

        builder.HasKey(x => x.Id).HasName("pk_tax_rates");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.TaxJurisdictionId).HasColumnName("tax_jurisdiction_id").IsRequired();
        builder.Property(x => x.TaxRateCode).HasColumnName("tax_rate_code").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.TaxRateName).HasColumnName("tax_rate_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.RatePercent).HasColumnName("rate_percent").HasPrecision(9, 4).IsRequired();
        builder.Property(x => x.IsCompound).HasColumnName("is_compound").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnName("valid_from").HasColumnType("date").IsRequired(false);
        builder.Property(x => x.ValidUntil).HasColumnName("valid_until").HasColumnType("date").IsRequired(false);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tax_rates_tenant_id_tenants");
        
        builder.HasOne<TaxJurisdiction>().WithMany().HasForeignKey(x => new { x.TenantId, x.TaxJurisdictionId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tax_rates_tax_jurisdiction_id_tax_jurisdictions");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tax_rates_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tax_rates_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.TaxRateCode }).IsUnique().HasDatabaseName("uq_tax_rates_tenant_id_tax_rate_code");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_tax_rates_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_tax_rates_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
            t.HasCheckConstraint("ck_tax_rates_rate_percent_min", "rate_percent >= 0");
            t.HasCheckConstraint("ck_tax_rates_rate_percent_max", "rate_percent <= 100");
            t.HasCheckConstraint("ck_tax_rates_valid_period", "valid_until IS NULL OR valid_from IS NULL OR valid_until >= valid_from");
        });
    }
}



