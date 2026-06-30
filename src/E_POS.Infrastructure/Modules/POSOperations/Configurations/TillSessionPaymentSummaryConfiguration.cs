using E_POS.Domain.Modules.Payment.Entities;
using E_POS.Domain.Modules.POSOperations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.POSOperations.Configurations;

public sealed class TillSessionPaymentSummaryConfiguration : IEntityTypeConfiguration<TillSessionPaymentSummary>
{
    public void Configure(EntityTypeBuilder<TillSessionPaymentSummary> builder)
    {
        builder.ToTable("till_session_payment_summaries");

        builder.HasKey(x => x.Id).HasName("pk_till_session_payment_summaries");

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

        builder.Property(x => x.PaymentMethodId)
            .HasColumnName("payment_method_id")
            .IsRequired();

        builder.Property(x => x.TillSessionSummaryId)
            .HasColumnName("till_session_summary_id")
            .IsRequired();

        builder.HasOne<TillSessionSummary>()
            .WithMany()
            .HasForeignKey(x => x.TillSessionSummaryId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_payment_summaries_till_session_summary_id_till_session_summaries");

        builder.HasOne<PaymentMethod>()
            .WithMany()
            .HasForeignKey(x => x.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_payment_summaries_payment_method_id_payment_methods");

        builder.HasIndex(x => new { x.TillSessionSummaryId, x.PaymentMethodId })
            .IsUnique()
            .HasDatabaseName("uq_till_session_payment_summaries_till_session_summary_id_payment_method_id");
    }
}

