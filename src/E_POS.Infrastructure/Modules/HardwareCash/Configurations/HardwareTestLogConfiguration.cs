using E_POS.Domain.Modules.HardwareCash.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.HardwareCash.Configurations;

public sealed class HardwareTestLogConfiguration : IEntityTypeConfiguration<HardwareTestLog>
{
    public void Configure(EntityTypeBuilder<HardwareTestLog> builder)
    {
        builder.ToTable("hardware_test_logs");

        builder.HasKey(x => x.Id).HasName("pk_hardware_test_logs");

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

        builder.Property(x => x.HardwareDeviceId)
            .HasColumnName("hardware_device_id")
            .IsRequired();

        builder.Property(x => x.TestResult)
            .HasColumnName("test_result")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.HasOne<HardwareDevice>()
            .WithMany()
            .HasForeignKey(x => x.HardwareDeviceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_hardware_test_logs_hardware_device_id_hardware_devices");

        builder.ToTable(t => t.HasCheckConstraint("ck_hardware_test_logs_test_result", "test_result IN ('SUCCESS', 'FAILED', 'WARNING')")); 
    }
}

