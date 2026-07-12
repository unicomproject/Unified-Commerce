using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.Customer.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(x => x.Id).HasName("pk_customers");

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

        builder.Property(x => x.CustomerCode)
            .HasColumnName("customer_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasColumnName("first_name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .HasColumnName("last_name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100);

        builder.Property(x => x.Name)
            .HasColumnName("display_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.Phone)
            .HasColumnName("phone")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50);

        builder.Property(x => x.NormalizedPhone)
            .HasColumnName("normalized_phone")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50);

        builder.Property(x => x.SourceType)
            .HasColumnName("source_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.SourceSalesChannelId)
            .HasColumnName("source_sales_channel_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.AnonymizedAt)
            .HasColumnName("anonymized_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customers_tenant_id_tenants");

        builder.HasOne<SalesChannel>()
            .WithMany()
            .HasForeignKey(x => x.SourceSalesChannelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customers_source_sales_channel_id_sales_channels");

        builder.HasIndex(x => new { x.TenantId, x.CustomerCode })
            .IsUnique()
            .HasDatabaseName("uq_customers_tenant_id_customer_code");

        builder.HasIndex(x => new { x.TenantId, x.NormalizedEmail })
            .IsUnique()
            .HasDatabaseName("uq_customers_tenant_id_normalized_email")
            .HasFilter("normalized_email IS NOT NULL AND status <> 'DELETED'");

        builder.HasIndex(x => new { x.TenantId, x.NormalizedPhone })
            .IsUnique()
            .HasDatabaseName("uq_customers_tenant_id_normalized_phone")
            .HasFilter("normalized_phone IS NOT NULL AND status <> 'DELETED'");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_customers_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_customers_source_type", "source_type IN ('POS', 'ECOMMERCE', 'CLICK_AND_COLLECT', 'IMPORT', 'MANUAL')");
            t.HasCheckConstraint("ck_customers_status", "status IN ('ACTIVE', 'INACTIVE', 'BLOCKED', 'DELETED')");
        });
    }
}


