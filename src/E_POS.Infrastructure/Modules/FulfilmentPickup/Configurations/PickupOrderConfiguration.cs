using E_POS.Domain.Modules.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.FulfilmentPickup.Configurations;

public sealed class PickupOrderConfiguration : IEntityTypeConfiguration<PickupOrder>
{
    public void Configure(EntityTypeBuilder<PickupOrder> builder)
    {
        builder.ToTable("pickup_orders");

        builder.HasKey(x => x.Id).HasName("pk_pickup_orders");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.DocumentNumberSequenceId)
            .HasColumnName("document_number_sequence_id")
            .IsRequired(false);

        builder.Property(x => x.FulfillmentOrderId)
            .HasColumnName("fulfillment_order_id")
            .IsRequired();

        builder.Property(x => x.PickupSlotReservationId)
            .HasColumnName("pickup_slot_reservation_id")
            .IsRequired(false);

        builder.Property(x => x.PickupNumber)
            .HasColumnName("pickup_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.PickupContactName)
            .HasColumnName("pickup_contact_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.PickupContactPhone)
            .HasColumnName("pickup_contact_phone")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.PickupContactEmail)
            .HasColumnName("pickup_contact_email")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(x => x.PickupContactChannel)
            .HasColumnName("pickup_contact_channel")
            .IsRequired(false);

        builder.Property(x => x.PickupStatus)
            .HasColumnName("pickup_status")
            .IsRequired();

        builder.Property(x => x.PickupNote)
            .HasColumnName("pickup_note")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.PickupQrTokenHash)
            .HasColumnName("pickup_qr_token_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(x => x.PickupQrVersion)
            .HasColumnName("pickup_qr_version")
            .IsRequired(false);

        builder.Property(x => x.PickupQrExpiresAt)
            .HasColumnName("pickup_qr_expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.VerificationMethod)
            .HasColumnName("verification_method")
            .IsRequired(false);

        builder.Property(x => x.VerifiedByTenantUserId)
            .HasColumnName("verified_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.VerifiedAt)
            .HasColumnName("verified_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.CollectedAt)
            .HasColumnName("collected_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.PickupNumber })
            .IsUnique()
            .HasDatabaseName("ux_pickup_orders_917d8d64");

        builder.HasIndex(x => new { x.TenantId, x.FulfillmentOrderId })
            .IsUnique()
            .HasDatabaseName("ux_pickup_orders_9c361648");

        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_orders_4c9803c3");

        builder.HasOne<E_POS.Domain.Modules.Orders.Entities.DocumentNumberSequence>()
            .WithMany()
            .HasForeignKey(x => x.DocumentNumberSequenceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_orders_e45489f4");

        builder.HasOne<E_POS.Domain.Modules.FulfilmentPickup.Entities.FulfillmentOrder>()
            .WithMany()
            .HasForeignKey(x => x.FulfillmentOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_orders_7fd43ab8");

        builder.HasOne<E_POS.Domain.Modules.FulfilmentPickup.Entities.PickupSlotReservation>()
            .WithMany()
            .HasForeignKey(x => x.PickupSlotReservationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_orders_9460bb7b");

        builder.HasOne<E_POS.Domain.Modules.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.VerifiedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pickup_orders_551f3774");
        // </second-brain-constraints>
    }
}