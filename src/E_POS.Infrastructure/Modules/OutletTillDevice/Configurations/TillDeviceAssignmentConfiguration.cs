using E_POS.Domain.Modules.OutletTillDevice.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.OutletTillDevice.Configurations;

public sealed class TillDeviceAssignmentConfiguration : IEntityTypeConfiguration<TillDeviceAssignment>
{
    public void Configure(EntityTypeBuilder<TillDeviceAssignment> builder)
    {
        builder.ToTable("till_device_assignments");

        builder.HasKey(x => x.Id).HasName("pk_till_device_assignments");

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

        builder.Property(x => x.TillId)
            .HasColumnName("till_id")
            .IsRequired(false);

        builder.Property(x => x.PosDeviceId)
            .HasColumnName("pos_device_id")
            .IsRequired(false);

        builder.Property(x => x.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.HasOne<Till>()
            .WithMany()
            .HasForeignKey(x => x.TillId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_device_assignments_till_id_tills");

        builder.HasOne<PosDevice>()
            .WithMany()
            .HasForeignKey(x => x.PosDeviceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_device_assignments_pos_device_id_pos_devices");

        builder.HasIndex(x => new { x.TillId, x.PosDeviceId, x.EffectiveFrom })
            .IsUnique()
            .HasDatabaseName("uq_till_device_assignments_till_id_pos_device_id_effective_from");
    }
}

