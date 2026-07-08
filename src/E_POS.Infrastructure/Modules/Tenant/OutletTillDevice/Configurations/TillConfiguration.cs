using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Configurations;

public sealed class TillConfiguration : IEntityTypeConfiguration<Till>
{
    public void Configure(EntityTypeBuilder<Till> builder)
    {
        builder.ToTable("tills");

        builder.HasKey(x => x.Id).HasName("pk_tills");

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

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id")
            .IsRequired(false);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.TillAreaName)
            .HasColumnName("till_area_name")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.TillNumber)
            .HasColumnName("till_number")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.TillCode)
            .HasColumnName("till_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => x.OutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tills_outlet_id_outlets");

        builder.HasIndex(x => new { x.TenantId, x.OutletId, x.TillCode })
            .IsUnique()
            .HasDatabaseName("uq_tills_tenant_id_outlet_id_till_code");

        builder.HasIndex(x => new { x.TenantId, x.OutletId, x.TillAreaName, x.TillNumber })
            .IsUnique()
            .HasDatabaseName("uq_tills_tenant_id_outlet_id_till_area_name_till_number");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_tills_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_tills_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
            t.HasCheckConstraint("ck_tills_till_number_positive", "till_number > 0");
        });
    }
}



