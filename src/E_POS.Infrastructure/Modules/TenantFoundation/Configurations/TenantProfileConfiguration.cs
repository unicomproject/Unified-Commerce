using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.TenantFoundation.Configurations;

public sealed class TenantProfileConfiguration : IEntityTypeConfiguration<TenantProfile>
{
    public void Configure(EntityTypeBuilder<TenantProfile> builder)
    {
        builder.ToTable("tenant_profiles");

        builder.HasKey(x => x.Id).HasName("pk_tenant_profiles");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.LegalName).HasColumnName("legal_name").HasColumnType("varchar(200)").HasMaxLength(200);
        builder.Property(x => x.RegistrationNumber).HasColumnName("registration_number").HasColumnType("varchar(80)").HasMaxLength(80);
        builder.Property(x => x.TaxNumber).HasColumnName("tax_number").HasColumnType("varchar(80)").HasMaxLength(80);
        builder.Property(x => x.PrimaryContactName).HasColumnName("primary_contact_name").HasColumnType("varchar(200)").HasMaxLength(200);
        builder.Property(x => x.PrimaryEmail).HasColumnName("primary_email").HasColumnType("varchar(255)").HasMaxLength(255);
        builder.Property(x => x.PrimaryPhone).HasColumnName("primary_phone").HasColumnType("varchar(40)").HasMaxLength(40);
        builder.Property(x => x.WebsiteUrl).HasColumnName("website_url").HasColumnType("varchar(255)").HasMaxLength(255);
        builder.Property(x => x.CountryCode).HasColumnName("country_code").HasColumnType("char(2)").HasMaxLength(2);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_profiles_tenant_id_tenants");

        builder.HasIndex(x => x.TenantId).IsUnique().HasDatabaseName("uq_tenant_profiles_tenant_id");
    }
}
