using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionCreditNoteLineConfiguration : IEntityTypeConfiguration<SubscriptionCreditNoteLine>
{
    public void Configure(EntityTypeBuilder<SubscriptionCreditNoteLine> builder)
    {
        builder.ToTable("subscription_credit_note_lines");

        builder.HasKey(x => x.Id).HasName("pk_subscription_credit_note_lines");

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

        builder.Property(x => x.LineCreditAmount)
            .HasColumnName("line_credit_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.LineNumber)
            .HasColumnName("line_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.SubscriptionCreditNoteId)
            .HasColumnName("subscription_credit_note_id")
            .IsRequired();

        builder.HasOne<SubscriptionCreditNote>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionCreditNoteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_credit_note_lines_subscription_credit_note_id_subscription_credit_notes");

        builder.HasIndex(x => new { x.SubscriptionCreditNoteId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("uq_subscription_credit_note_lines_subscription_credit_note_id_line_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_credit_note_lines_line_credit_amount", "line_credit_amount >= 0")); 
    }
}



