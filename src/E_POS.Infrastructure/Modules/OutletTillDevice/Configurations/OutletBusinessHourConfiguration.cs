using E_POS.Domain.Modules.OutletTillDevice.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.OutletTillDevice.Configurations;

public sealed class OutletBusinessHourConfiguration : IEntityTypeConfiguration<OutletBusinessHour>
{
    public void Configure(EntityTypeBuilder<OutletBusinessHour> builder)
    {
        builder.ToTable("outlet_business_hours");

        builder.HasKey(x => x.Id).HasName("pk_outlet_business_hours");

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

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id")
            .IsRequired(false);

        builder.Property(x => x.DayOfWeek)
            .HasColumnName("day_of_week");

        builder.Property(x => x.OpenTime)
            .HasColumnName("open_time")
            .HasColumnType("time without time zone");

        builder.Property(x => x.CloseTime)
            .HasColumnName("close_time")
            .HasColumnType("time without time zone");

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => x.OutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_outlet_business_hours_outlet_id_outlets");

        builder.HasIndex(x => new { x.OutletId, x.DayOfWeek })
            .IsUnique()
            .HasDatabaseName("uq_outlet_business_hours_outlet_id_day_of_week");

        builder.ToTable(t => t.HasCheckConstraint("ck_outlet_business_hours_day_of_week", "day_of_week BETWEEN 0 AND 6")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_outlet_business_hours_open_time_close_time", "open_time < close_time")); 
    }
}

