using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Configurations;

public sealed class CashCountDenominationConfiguration : IEntityTypeConfiguration<CashCountDenomination>
{
    public void Configure(EntityTypeBuilder<CashCountDenomination> builder)
    {
        builder.ToTable("cash_count_denominations");

        builder.HasKey(x => x.Id).HasName("pk_cash_count_denominations");

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

        builder.Property(x => x.CashReconciliationId)
            .HasColumnName("cash_reconciliation_id")
            .IsRequired();

        builder.Property(x => x.DenominationValue)
            .HasColumnName("denomination_value")
            .HasPrecision(18, 2);

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4);

        builder.HasOne<CashReconciliation>()
            .WithMany()
            .HasForeignKey(x => x.CashReconciliationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cash_count_denominations_cash_reconciliation_id_cash_reconciliations");

        builder.HasIndex(x => new { x.CashReconciliationId, x.DenominationValue })
            .IsUnique()
            .HasDatabaseName("uq_cash_count_denominations_cash_reconciliation_id_denomination_value");

        builder.ToTable(t => t.HasCheckConstraint("ck_cash_count_denominations_denomination_value", "denomination_value > 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_cash_count_denominations_quantity", "quantity >= 0")); 
    }
}



