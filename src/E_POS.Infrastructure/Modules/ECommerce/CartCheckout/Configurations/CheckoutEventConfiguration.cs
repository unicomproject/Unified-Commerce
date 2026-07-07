using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Configurations;

public sealed class CheckoutEventConfiguration : IEntityTypeConfiguration<CheckoutEvent>
{
    public void Configure(EntityTypeBuilder<CheckoutEvent> builder)
    {
        builder.ToTable("checkout_events");

        builder.HasKey(x => x.Id).HasName("pk_checkout_events");

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

        builder.Property(x => x.CheckoutSessionId)
            .HasColumnName("checkout_session_id")
            .IsRequired(false);

        builder.Property(x => x.SequenceNumber)
            .HasColumnName("sequence_number");

        builder.HasOne<CheckoutSession>()
            .WithMany()
            .HasForeignKey(x => x.CheckoutSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_events_checkout_session_id_checkout_sessions");

        builder.HasIndex(x => new { x.CheckoutSessionId, x.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("uq_checkout_events_checkout_session_id_sequence_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_checkout_events_sequence_number", "sequence_number > 0")); 
    }
}



