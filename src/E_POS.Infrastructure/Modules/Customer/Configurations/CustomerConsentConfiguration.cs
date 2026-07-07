using E_POS.Domain.Modules.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.Customer.Entities.Customer;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Customer.Configurations;

public sealed class CustomerConsentConfiguration : IEntityTypeConfiguration<CustomerConsent>
{
    public void Configure(EntityTypeBuilder<CustomerConsent> builder)
    {
        builder.ToTable("customer_consents");

        builder.HasKey(x => x.Id).HasName("pk_customer_consents");

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

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(x => x.ConsentType)
            .HasColumnName("consent_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.PolicyVersion)
            .HasColumnName("policy_version")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50);

        builder.Property(x => x.ConsentStatus)
            .HasColumnName("consent_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ConsentSource)
            .HasColumnName("consent_source")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.SalesChannelId)
            .HasColumnName("sales_channel_id");

        builder.Property(x => x.RecordedAt)
            .HasColumnName("recorded_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.WithdrawnAt)
            .HasColumnName("withdrawn_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("inet");

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasColumnType("text");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_consents_tenant_id_tenants");

        builder.HasOne<CustomerEntity>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.CustomerId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_consents_customer_id_customers");

        builder.HasOne<SalesChannel>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesChannelId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_consents_sales_channel_id_sales_channels");

        builder.HasIndex(x => new { x.TenantId, x.CustomerId, x.ConsentType, x.SalesChannelId })
            .IsUnique()
            .HasDatabaseName("uq_customer_consents_tenant_id_customer_id_consent_type_sales_channel_id");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_customer_consents_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_customer_consents_consent_type", "consent_type IN ('MARKETING_EMAIL', 'MARKETING_SMS', 'MARKETING_WHATSAPP', 'TERMS', 'PRIVACY')");
            t.HasCheckConstraint("ck_customer_consents_consent_status", "consent_status IN ('GRANTED', 'WITHDRAWN', 'EXPIRED')");
            t.HasCheckConstraint("ck_customer_consents_consent_source", "consent_source IN ('POS', 'ECOMMERCE', 'CLICK_AND_COLLECT', 'ADMIN', 'IMPORT')");
        });
    }
}


