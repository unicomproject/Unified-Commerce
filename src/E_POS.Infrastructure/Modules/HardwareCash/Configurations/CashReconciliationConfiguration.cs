using E_POS.Domain.Modules.HardwareCash.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.HardwareCash.Configurations;

public sealed class CashReconciliationConfiguration : IEntityTypeConfiguration<CashReconciliation>
{
    public void Configure(EntityTypeBuilder<CashReconciliation> builder)
    {
        builder.ToTable("cash_reconciliations");

        builder.HasKey(x => x.Id).HasName("pk_cash_reconciliations");

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

        builder.Property(x => x.CountedCashAmount)
            .HasColumnName("counted_cash_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.ExpectedCashAmount)
            .HasColumnName("expected_cash_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.TillSessionId)
            .HasColumnName("till_session_id")
            .IsRequired();

        builder.HasOne<TillSession>()
            .WithMany()
            .HasForeignKey(x => x.TillSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cash_reconciliations_till_session_id_till_sessions");

        builder.HasIndex(x => x.TillSessionId)
            .IsUnique()
            .HasDatabaseName("uq_cash_reconciliations_till_session_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_cash_reconciliations_expected_cash_amount", "expected_cash_amount >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_cash_reconciliations_counted_cash_amount", "counted_cash_amount >= 0")); 
    }
}

