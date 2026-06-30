using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class StocktakeLineSerialConfiguration : IEntityTypeConfiguration<StocktakeLineSerial>
{
    public void Configure(EntityTypeBuilder<StocktakeLineSerial> builder)
    {
        builder.ToTable("stocktake_line_serials");

        builder.HasKey(x => x.Id).HasName("pk_stocktake_line_serials");

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

        builder.Property(x => x.SerialNumberId)
            .HasColumnName("serial_number_id")
            .IsRequired();

        builder.Property(x => x.StocktakeLineId)
            .HasColumnName("stocktake_line_id")
            .IsRequired();

        builder.HasOne<StocktakeLine>()
            .WithMany()
            .HasForeignKey(x => x.StocktakeLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stocktake_line_serials_stocktake_line_id_stocktake_lines");

        builder.HasOne<SerialNumber>()
            .WithMany()
            .HasForeignKey(x => x.SerialNumberId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stocktake_line_serials_serial_number_id_serial_numbers");

        builder.HasIndex(x => new { x.StocktakeLineId, x.SerialNumberId })
            .IsUnique()
            .HasDatabaseName("uq_stocktake_line_serials_stocktake_line_id_serial_number_id");
    }
}

