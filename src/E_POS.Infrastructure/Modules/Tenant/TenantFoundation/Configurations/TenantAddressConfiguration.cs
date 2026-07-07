using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Configurations;

public sealed class TenantAddressConfiguration : IEntityTypeConfiguration<TenantAddress>
{
    public void Configure(EntityTypeBuilder<TenantAddress> builder)
    {
        builder.ToTable("tenant_addresses");

        builder.HasKey(x => x.Id).HasName("pk_tenant_addresses");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.AddressType).HasColumnName("address_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        
        builder.Property(x => x.AddressLine1).HasColumnName("address_line1").HasColumnType("varchar(250)").HasMaxLength(250).IsRequired();
        builder.Property(x => x.AddressLine2).HasColumnName("address_line2").HasColumnType("varchar(250)").HasMaxLength(250);
        builder.Property(x => x.City).HasColumnName("city").HasColumnType("varchar(120)").HasMaxLength(120);
        builder.Property(x => x.StateOrProvince).HasColumnName("state_or_province").HasColumnType("varchar(120)").HasMaxLength(120);
        builder.Property(x => x.PostalCode).HasColumnName("postal_code").HasColumnType("varchar(30)").HasMaxLength(30);
        builder.Property(x => x.CountryCode).HasColumnName("country_code").HasColumnType("char(2)").HasMaxLength(2).IsRequired();
        
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        
        builder.Property(x => x.CreatedByPlatformUserId).HasColumnName("created_by_platform_user_id");
        builder.Property(x => x.UpdatedByPlatformUserId).HasColumnName("updated_by_platform_user_id");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_addresses_tenant_id_tenants");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_tenant_addresses_address_type",
            "address_type IN ('BILLING', 'REGISTERED', 'CONTACT')"));
    }
}
