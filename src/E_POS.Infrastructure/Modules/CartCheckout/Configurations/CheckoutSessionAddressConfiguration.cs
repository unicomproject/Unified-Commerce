using E_POS.Domain.Modules.CartCheckout.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CartCheckout.Configurations;

public sealed class CheckoutSessionAddressConfiguration : IEntityTypeConfiguration<CheckoutSessionAddress>
{
    public void Configure(EntityTypeBuilder<CheckoutSessionAddress> builder)
    {
        builder.ToTable("checkout_session_addresses");

        builder.HasKey(x => x.Id).HasName("pk_checkout_session_addresses");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.CheckoutSessionId).HasColumnName("checkout_session_id").IsRequired();
        builder.Property(x => x.AddressType).HasColumnName("address_type").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ContactName).HasColumnName("contact_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired(false);
        builder.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasColumnType("varchar(50)").HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.AddressLine1).HasColumnName("address_line1").HasColumnType("varchar(200)").HasMaxLength(200).IsRequired();
        builder.Property(x => x.AddressLine2).HasColumnName("address_line2").HasColumnType("varchar(200)").HasMaxLength(200).IsRequired(false);
        builder.Property(x => x.City).HasColumnName("city").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.StateOrProvince).HasColumnName("state_or_province").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.PostalCode).HasColumnName("postal_code").HasColumnType("varchar(20)").HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.CountryCode).HasColumnName("country_code").HasColumnType("char(2)").HasMaxLength(2).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_checkout_session_addresses_tenant_id_tenants");
        builder.HasOne<CheckoutSession>().WithMany().HasForeignKey(x => x.CheckoutSessionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_checkout_session_addresses_checkout_session_id_checkout_sessions");
    }
}