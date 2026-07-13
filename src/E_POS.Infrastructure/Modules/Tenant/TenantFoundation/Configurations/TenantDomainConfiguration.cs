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

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.SalesChannelId).HasColumnName("sales_channel_id");

        builder.Property(x => x.DomainType).HasColumnName("domain_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.DomainName).HasColumnName("domain_name").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary").IsRequired();
        builder.Property(x => x.VerificationStatus).HasColumnName("verification_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.VerificationTokenHash).HasColumnName("verification_token_hash").HasColumnType("varchar(255)").HasMaxLength(255);
        builder.Property(x => x.VerifiedAt).HasColumnName("verified_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.SslStatus).HasColumnName("ssl_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.SslIssuedAt).HasColumnName("ssl_issued_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.SslExpiresAt).HasColumnName("ssl_expires_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CreatedByPlatformUserId).HasColumnName("created_by_platform_user_id");
        builder.Property(x => x.UpdatedByPlatformUserId).HasColumnName("updated_by_platform_user_id");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_domains_tenant_id_tenants");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>()
            .WithMany()
            .HasForeignKey(x => x.SalesChannelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_domains_sales_channel_id_sales_channels");

        builder.HasIndex(x => x.DomainName)
            .IsUnique()
            .HasDatabaseName("uq_tenant_domains_domain_name");

        builder.HasIndex(x => x.SalesChannelId)
            .HasDatabaseName("ix_tenant_domains_sales_channel_id");
            
        builder.HasIndex(x => x.VerificationTokenHash)
            .IsUnique()
            .HasFilter("verification_token_hash IS NOT NULL")
            .HasDatabaseName("uq_tenant_domains_verification_token_hash");
    }
}




