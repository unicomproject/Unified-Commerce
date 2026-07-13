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

        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by_tenant_user_id");

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.CheckoutSessionId)
            .HasColumnName("checkout_session_id")
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.EventStatus)
            .HasColumnName("event_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.EventPayloadJson)
            .HasColumnName("event_payload_json")
            .HasColumnType("jsonb");

        builder.Property(x => x.EventAt)
            .HasColumnName("event_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_events_tenant_id_tenants");

        builder.HasOne<CheckoutSession>()
            .WithMany()
            .HasForeignKey(x => x.CheckoutSessionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_checkout_events_checkout_session_id_checkout_sessions");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_events_created_by_tenant_user_id_tenant_users");
    }
}



