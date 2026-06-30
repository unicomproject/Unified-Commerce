using E_POS.Domain.Modules.OutletTillDevice.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.OutletTillDevice.Configurations;

public sealed class PosDeviceConfiguration : IEntityTypeConfiguration<PosDevice>
{
    public void Configure(EntityTypeBuilder<PosDevice> builder)
    {
        builder.ToTable("pos_devices");

        builder.HasKey(x => x.Id).HasName("pk_pos_devices");

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

        builder.Property(x => x.DeviceCode)
            .HasColumnName("device_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.DeviceSerialNumber)
            .HasColumnName("device_serial_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => x.OutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pos_devices_outlet_id_outlets");

        builder.HasIndex(x => new { x.TenantId, x.DeviceCode })
            .IsUnique()
            .HasDatabaseName("uq_pos_devices_tenant_id_device_code");

        builder.HasIndex(x => x.DeviceSerialNumber)
            .IsUnique()
            .HasDatabaseName("uq_pos_devices_device_serial_number")
            .HasFilter("device_serial_number IS NOT NULL");
    }
}

