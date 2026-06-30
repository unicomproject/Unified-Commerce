using E_POS.Domain.Modules.PricingTax.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.PricingTax.Configurations;

public sealed class TaxClassRateConfiguration : IEntityTypeConfiguration<TaxClassRate>
{
    public void Configure(EntityTypeBuilder<TaxClassRate> builder)
    {
        builder.ToTable("tax_class_rates");

        builder.HasKey(x => x.Id).HasName("pk_tax_class_rates");

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

        builder.Property(x => x.TaxClassId)
            .HasColumnName("tax_class_id")
            .IsRequired();

        builder.Property(x => x.TaxRateId)
            .HasColumnName("tax_rate_id")
            .IsRequired();

        builder.HasOne<TaxClass>()
            .WithMany()
            .HasForeignKey(x => x.TaxClassId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tax_class_rates_tax_class_id_tax_classes");

        builder.HasOne<TaxRate>()
            .WithMany()
            .HasForeignKey(x => x.TaxRateId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tax_class_rates_tax_rate_id_tax_rates");

        builder.HasIndex(x => new { x.TaxClassId, x.TaxRateId })
            .IsUnique()
            .HasDatabaseName("uq_tax_class_rates_tax_class_id_tax_rate_id");
    }
}

