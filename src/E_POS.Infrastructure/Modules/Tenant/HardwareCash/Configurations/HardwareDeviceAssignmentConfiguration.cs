using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Configurations;

public sealed class HardwareDeviceAssignmentConfiguration : IEntityTypeConfiguration<HardwareDeviceAssignment>
{
    public void Configure(EntityTypeBuilder<HardwareDeviceAssignment> builder)
    {
        builder.ToTable("hardware_device_assignments");

        builder.HasKey(x => x.Id).HasName("pk_hardware_device_assignments");

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

        builder.Property(x => x.PosDeviceId)
            .HasColumnName("pos_device_id")
            .IsRequired(false);

        builder.Property(x => x.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.HardwareDeviceId)
            .HasColumnName("hardware_device_id")
            .IsRequired();

        builder.HasOne<HardwareDevice>()
            .WithMany()
            .HasForeignKey(x => x.HardwareDeviceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_hardware_device_assignments_hardware_device_id_hardware_devices");

        builder.HasOne<PosDevice>()
            .WithMany()
            .HasForeignKey(x => x.PosDeviceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_hardware_device_assignments_pos_device_id_pos_devices");

        builder.HasIndex(x => new { x.HardwareDeviceId, x.PosDeviceId, x.EffectiveFrom })
            .IsUnique()
            .HasDatabaseName("uq_hardware_device_assignments_hardware_device_id_pos_device_id_effective_from");
    }
}



