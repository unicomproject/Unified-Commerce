using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

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

        builder.Property(x => x.UomCode)
            .HasColumnName("uom_code")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.UomName)
            .HasColumnName("uom_name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.UomType)
            .HasColumnName("uom_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Symbol)
            .HasColumnName("symbol")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(x => x.BaseUomId)
            .HasColumnName("base_uom_id")
            .IsRequired(false);

        builder.Property(x => x.ConversionFactor)
            .HasColumnName("conversion_factor")
            .HasColumnType("numeric(18,6)")
            .HasDefaultValue(1m)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_unit_of_measures_tenant_id_tenants");

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => x.BaseUomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_unit_of_measures_base_uom_id_unit_of_measures");

        builder.HasIndex(x => x.UomCode)
            .IsUnique()
            .HasDatabaseName("uq_unit_of_measures_global_uom_code")
            .HasFilter("tenant_id IS NULL");

        builder.HasIndex(x => new { x.TenantId, x.UomCode })
            .IsUnique()
            .HasDatabaseName("uq_unit_of_measures_tenant_id_uom_code")
            .HasFilter("tenant_id IS NOT NULL");

        builder.ToTable(t => t.HasCheckConstraint("ck_unit_of_measures_conversion_factor", "conversion_factor > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_unit_of_measures_base_uom_id", "base_uom_id IS NULL OR base_uom_id <> id"));
        builder.ToTable(t => t.HasCheckConstraint("ck_unit_of_measures_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}


