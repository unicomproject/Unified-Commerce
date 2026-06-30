using E_POS.Domain.Modules.HardwareCash.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.HardwareCash.Configurations;

public sealed class HardwareDeviceConfiguration : IEntityTypeConfiguration<HardwareDevice>
{
    public void Configure(EntityTypeBuilder<HardwareDevice> builder)
    {
        builder.ToTable("hardware_devices");

        builder.HasKey(x => x.Id).HasName("pk_hardware_devices");

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
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.HardwareDeviceCode)
            .HasColumnName("hardware_device_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.SerialNumber)
            .HasColumnName("serial_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => x.OutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_hardware_devices_outlet_id_outlets");

        builder.HasIndex(x => new { x.TenantId, x.HardwareDeviceCode })
            .IsUnique()
            .HasDatabaseName("uq_hardware_devices_tenant_id_hardware_device_code");

        builder.HasIndex(x => x.SerialNumber)
            .IsUnique()
            .HasDatabaseName("uq_hardware_devices_serial_number")
            .HasFilter("serial_number IS NOT NULL");
    }
}

