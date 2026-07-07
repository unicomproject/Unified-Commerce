using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Configurations;

public sealed class TenantDomainConfiguration : IEntityTypeConfiguration<TenantDomain>
{
    public void Configure(EntityTypeBuilder<TenantDomain> builder)
    {
        builder.ToTable("tenant_domains");

        builder.HasKey(x => x.Id).HasName("pk_tenant_domains");

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

        builder.Property(x => x.DomainName)
            .HasColumnName("domain_name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200);

        builder.Property(x => x.DomainStatus)
            .HasColumnName("domain_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_domains_tenant_id_tenants");

        builder.HasIndex(x => x.DomainName)
            .IsUnique()
            .HasDatabaseName("uq_tenant_domains_domain_name");

        builder.ToTable(t => t.HasCheckConstraint("ck_tenant_domains_domain_status", "domain_status IN ('PENDING', 'VERIFIED', 'FAILED', 'DISABLED')")); 
    }
}




