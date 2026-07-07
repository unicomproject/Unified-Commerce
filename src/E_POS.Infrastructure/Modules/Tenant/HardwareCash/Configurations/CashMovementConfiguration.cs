using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Configurations;

public sealed class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("cash_movements");

        builder.HasKey(x => x.Id).HasName("pk_cash_movements");

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

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.CashMovementTypeId)
            .HasColumnName("cash_movement_type_id")
            .IsRequired();

        builder.Property(x => x.TillSessionId)
            .HasColumnName("till_session_id")
            .IsRequired();

        builder.HasOne<TillSession>()
            .WithMany()
            .HasForeignKey(x => x.TillSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cash_movements_till_session_id_till_sessions");

        builder.HasOne<CashMovementType>()
            .WithMany()
            .HasForeignKey(x => x.CashMovementTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cash_movements_cash_movement_type_id_cash_movement_types");

        builder.ToTable(t => t.HasCheckConstraint("ck_cash_movements_amount", "amount > 0")); 
    }
}



