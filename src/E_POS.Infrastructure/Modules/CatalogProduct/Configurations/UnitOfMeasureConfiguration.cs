using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("unit_of_measures");

        builder.HasKey(x => x.Id).HasName("pk_unit_of_measures");

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
            .IsRequired(false);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ConversionFactor)
            .HasColumnName("conversion_factor")
            .HasPrecision(18, 4);

        builder.Property(x => x.UomCode)
            .HasColumnName("uom_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_unit_of_measures_tenant_id_tenants");

        builder.HasIndex(x => x.UomCode)
            .IsUnique()
            .HasDatabaseName("uq_unit_of_measures_global_uom_code")
            .HasFilter("tenant_id IS NULL");

        builder.HasIndex(x => new { x.TenantId, x.UomCode })
            .IsUnique()
            .HasDatabaseName("uq_unit_of_measures_tenant_id_uom_code")
            .HasFilter("tenant_id IS NOT NULL");

        builder.ToTable(t => t.HasCheckConstraint("ck_unit_of_measures_conversion_factor", "conversion_factor IS NULL OR conversion_factor > 0"));
    }
}