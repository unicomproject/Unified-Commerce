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

        builder.Ignore(x => x.UpdatedAt);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.SalesPaymentId)
            .HasColumnName("sales_payment_id")
            .IsRequired();

        builder.Property(x => x.SequenceNumber)
            .HasColumnName("sequence_number")
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .IsRequired();

        builder.Property(x => x.OldStatus)
            .HasColumnName("old_status")
            .IsRequired(false);

        builder.Property(x => x.NewStatus)
            .HasColumnName("new_status")
            .IsRequired(false);

        builder.Property(x => x.EventNote)
            .HasColumnName("event_note")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.EventPayloadJson)
            .HasColumnName("event_payload_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.EventAt)
            .HasColumnName("event_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.EventByTenantUserId)
            .HasColumnName("event_by_tenant_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.SalesPaymentId, x.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("ux_sales_payment_events_f38768c9");

        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payment_events_7033c252");

        builder.HasOne<E_POS.Domain.Modules.Payment.Entities.SalesPayment>()
            .WithMany()
            .HasForeignKey(x => x.SalesPaymentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payment_events_832fd9c0");

        builder.HasOne<E_POS.Domain.Modules.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.EventByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payment_events_ea76aac1");
        // <second-brain-checks>
        builder.ToTable(t => t.HasCheckConstraint("ck_sales_payment_events_a1a5b828", "sequence_number > 0"));
        // </second-brain-checks>

        // </second-brain-constraints>
    }
}