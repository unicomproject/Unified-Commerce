using E_POS.Domain.Modules.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.Customer.Entities.Customer;
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
            .HasColumnName("tenant_id");

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired(false);

        builder.Property(x => x.ConsentType)
            .HasColumnName("consent_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.SalesChannelId)
            .HasColumnName("sales_channel_id");

        builder.HasOne<CustomerEntity>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customer_consents_customer_id_customers");

        builder.HasIndex(x => new { x.TenantId, x.CustomerId, x.ConsentType, x.SalesChannelId })
            .IsUnique()
            .HasDatabaseName("uq_customer_consents_tenant_id_customer_id_consent_type_sales_channel_id");
    }
}


