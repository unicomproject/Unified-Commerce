using E_POS.Domain.Modules.HardwareCash.Entities;
using E_POS.Domain.Modules.POSOperations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.POSOperations.Configurations;

public sealed class TillSessionSummaryConfiguration : IEntityTypeConfiguration<TillSessionSummary>
{
    public void Configure(EntityTypeBuilder<TillSessionSummary> builder)
    {
        builder.ToTable("till_session_summaries");

        builder.HasKey(x => x.Id).HasName("pk_till_session_summaries");

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

        builder.Property(x => x.OrderCount)
            .HasColumnName("order_count");

        builder.Property(x => x.RefundCount)
            .HasColumnName("refund_count");

        builder.Property(x => x.TillSessionId)
            .HasColumnName("till_session_id")
            .IsRequired();

        builder.HasOne<TillSession>()
            .WithMany()
            .HasForeignKey(x => x.TillSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_summaries_till_session_id_till_sessions");

        builder.HasIndex(x => x.TillSessionId)
            .IsUnique()
            .HasDatabaseName("uq_till_session_summaries_till_session_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_till_session_summaries_order_count", "order_count >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_till_session_summaries_refund_count", "refund_count >= 0")); 
    }
}

