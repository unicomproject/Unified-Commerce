using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Configurations;

public sealed class PaymentProviderWebhookEventConfiguration : IEntityTypeConfiguration<PaymentProviderWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PaymentProviderWebhookEvent> builder)
    {
        builder.ToTable("payment_provider_webhook_events");

        builder.HasKey(x => x.Id).HasName("pk_payment_provider_webhook_events");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Provider)
            .HasColumnName("provider")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ExternalEventId)
            .HasColumnName("external_event_id")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.ReceivedAt)
            .HasColumnName("received_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => new { x.Provider, x.ExternalEventId })
            .IsUnique()
            .HasDatabaseName("ux_payment_provider_webhook_events_provider_event_id");
    }
}
