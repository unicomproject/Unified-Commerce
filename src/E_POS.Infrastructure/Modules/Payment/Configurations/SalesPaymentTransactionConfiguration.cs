using E_POS.Domain.Modules.Payment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Payment.Configurations;

public sealed class SalesPaymentTransactionConfiguration : IEntityTypeConfiguration<SalesPaymentTransaction>
{
    public void Configure(EntityTypeBuilder<SalesPaymentTransaction> builder)
    {
        builder.ToTable("sales_payment_transactions");

        builder.HasKey(x => x.Id).HasName("pk_sales_payment_transactions");

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

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120);

        builder.Property(x => x.SalesPaymentId)
            .HasColumnName("sales_payment_id")
            .IsRequired();

        builder.HasOne<SalesPayment>()
            .WithMany()
            .HasForeignKey(x => x.SalesPaymentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payment_transactions_sales_payment_id_sales_payments");

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("uq_sales_payment_transactions_idempotency_key")
            .HasFilter("idempotency_key IS NOT NULL");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_payment_transactions_amount", "amount >= 0")); 
    }
}

