using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.TenantFoundation.Configurations;

public sealed class TenantAddressConfiguration : IEntityTypeConfiguration<TenantAddress>
{
    public void Configure(EntityTypeBuilder<TenantAddress> builder)
    {
        builder.ToTable("tenant_addresses");

        builder.HasKey(x => x.Id).HasName("pk_tenant_addresses");

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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.AddressType)
            .HasColumnName("address_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_addresses_tenant_id_tenants");

        builder.ToTable(t => t.HasCheckConstraint("ck_tenant_addresses_address_type", "address_type IN ('BILLING', 'REGISTERED', 'CONTACT')")); 
    }
}

