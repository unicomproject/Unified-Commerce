using E_POS.Domain.Modules.PricingTax.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.PricingTax.Configurations;

public sealed class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> builder)
    {
        builder.ToTable("tax_rates");

        builder.HasKey(x => x.Id).HasName("pk_tax_rates");

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

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.RatePercent)
            .HasColumnName("rate_percent")
            .HasPrecision(9, 4);

        builder.Property(x => x.TaxJurisdictionId)
            .HasColumnName("tax_jurisdiction_id")
            .IsRequired();

        builder.Property(x => x.TaxRateCode)
            .HasColumnName("tax_rate_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.HasOne<TaxJurisdiction>()
            .WithMany()
            .HasForeignKey(x => x.TaxJurisdictionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tax_rates_tax_jurisdiction_id_tax_jurisdictions");

        builder.HasIndex(x => new { x.TaxJurisdictionId, x.TaxRateCode })
            .IsUnique()
            .HasDatabaseName("uq_tax_rates_tax_jurisdiction_id_tax_rate_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_tax_rates_rate_percent", "rate_percent >= 0")); 
    }
}

