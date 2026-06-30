using E_POS.Domain.Modules.Payment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Payment.Configurations;

public sealed class SalesPaymentEventConfiguration : IEntityTypeConfiguration<SalesPaymentEvent>
{
    public void Configure(EntityTypeBuilder<SalesPaymentEvent> builder)
    {
        builder.ToTable("sales_payment_events");

        builder.HasKey(x => x.Id).HasName("pk_sales_payment_events");

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

        builder.Property(x => x.SalesPaymentId)
            .HasColumnName("sales_payment_id")
            .IsRequired();

        builder.Property(x => x.SequenceNumber)
            .HasColumnName("sequence_number");

        builder.HasOne<SalesPayment>()
            .WithMany()
            .HasForeignKey(x => x.SalesPaymentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payment_events_sales_payment_id_sales_payments");

        builder.HasIndex(x => new { x.SalesPaymentId, x.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("uq_sales_payment_events_sales_payment_id_sequence_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_payment_events_sequence_number", "sequence_number > 0")); 
    }
}

