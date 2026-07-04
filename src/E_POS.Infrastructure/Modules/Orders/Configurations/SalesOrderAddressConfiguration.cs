using E_POS.Domain.Modules.Orders.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Orders.Configurations;

public sealed class SalesOrderAddressConfiguration : IEntityTypeConfiguration<SalesOrderAddress>
{
    public void Configure(EntityTypeBuilder<SalesOrderAddress> builder)
    {
        builder.ToTable("sales_order_addresses");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_addresses");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.SalesOrderId).HasColumnName("sales_order_id").IsRequired();
        builder.Property(x => x.AddressType).HasColumnName("address_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CustomerAddressId).HasColumnName("customer_address_id").IsRequired(false);
        builder.Property(x => x.ContactName).HasColumnName("contact_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasColumnType("varchar(50)").HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.AddressLine1).HasColumnName("address_line1").HasColumnType("varchar(200)").HasMaxLength(200).IsRequired();
        builder.Property(x => x.AddressLine2).HasColumnName("address_line2").HasColumnType("varchar(200)").HasMaxLength(200).IsRequired(false);
        builder.Property(x => x.City).HasColumnName("city").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.StateOrProvince).HasColumnName("state_or_province").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.PostalCode).HasColumnName("postal_code").HasColumnType("varchar(20)").HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.CountryCode).HasColumnName("country_code").HasColumnType("char(2)").HasMaxLength(2).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_sales_order_addresses_tenant_id_tenants");
        builder.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_sales_order_addresses_sales_order_id_sales_orders");
    }
}