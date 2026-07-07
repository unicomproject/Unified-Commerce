using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.PricingTax.Configurations;

public sealed class TaxJurisdictionConfiguration : IEntityTypeConfiguration<TaxJurisdiction>
{
    public void Configure(EntityTypeBuilder<TaxJurisdiction> builder)
    {
        builder.ToTable("tax_jurisdictions");

        builder.HasKey(x => x.Id).HasName("pk_tax_jurisdictions");

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
        builder.Property(x => x.ParentJurisdictionId).HasColumnName("parent_jurisdiction_id").IsRequired(false);
        builder.Property(x => x.JurisdictionCode).HasColumnName("jurisdiction_code").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.JurisdictionName).HasColumnName("jurisdiction_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.JurisdictionType).HasColumnName("jurisdiction_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CountryCode).HasColumnName("country_code").HasColumnType("char(2)").HasMaxLength(2).IsRequired();
        builder.Property(x => x.RegionCode).HasColumnName("region_code").HasColumnType("varchar(50)").HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.LocalityName).HasColumnName("locality_name").HasColumnType("varchar(120)").HasMaxLength(120).IsRequired(false);
        builder.Property(x => x.PostalCodePattern).HasColumnName("postal_code_pattern").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tax_jurisdictions_tenant_id_tenants");
        
        builder.HasOne<TaxJurisdiction>().WithMany().HasForeignKey(x => new { x.TenantId, x.ParentJurisdictionId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tax_jurisdictions_parent_jurisdiction_id_tax_jurisdictions");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tax_jurisdictions_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tax_jurisdictions_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.JurisdictionCode }).IsUnique().HasDatabaseName("uq_tax_jurisdictions_tenant_id_jurisdiction_code");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_tax_jurisdictions_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_tax_jurisdictions_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
            t.HasCheckConstraint("ck_tax_jurisdictions_parent", "parent_jurisdiction_id IS NULL OR parent_jurisdiction_id <> id");
        });
    }
}



