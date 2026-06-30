using E_POS.Domain.Modules.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.Customer.Entities.Customer;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Customer.Configurations;

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

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasColumnType("citext");

        builder.Property(x => x.NormalizedPhone)
            .HasColumnName("normalized_phone")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CustomerCode)
            .HasColumnName("customer_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_customers_tenant_id_tenants");

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
    }
}


