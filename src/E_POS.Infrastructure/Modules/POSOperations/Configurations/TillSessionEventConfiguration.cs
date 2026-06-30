using E_POS.Domain.Modules.HardwareCash.Entities;
using E_POS.Domain.Modules.POSOperations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.POSOperations.Configurations;

public sealed class TillSessionEventConfiguration : IEntityTypeConfiguration<TillSessionEvent>
{
    public void Configure(EntityTypeBuilder<TillSessionEvent> builder)
    {
        builder.ToTable("till_session_events");

        builder.HasKey(x => x.Id).HasName("pk_till_session_events");

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

        builder.Property(x => x.TillSessionId)
            .HasColumnName("till_session_id")
            .IsRequired();

        builder.HasOne<TillSession>()
            .WithMany()
            .HasForeignKey(x => x.TillSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_events_till_session_id_till_sessions");

        builder.ToTable(t => t.HasCheckConstraint("ck_till_session_events_amount", "amount IS NULL OR amount >= 0")); 
    }
}

