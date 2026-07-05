using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.HardwareCash.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Domain.Modules.POSOperations.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.TillSessionId)
            .HasColumnName("till_session_id")
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.EventAt)
            .HasColumnName("event_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.EventByTenantUserId)
            .HasColumnName("event_by_tenant_user_id");

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 4);

        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3);

        builder.Property(x => x.ReferenceType)
            .HasColumnName("reference_type")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50);

        builder.Property(x => x.ReferenceId)
            .HasColumnName("reference_id");

        builder.Property(x => x.EventPayloadJson)
            .HasColumnName("event_payload_json")
            .HasColumnType("jsonb");

        builder.Property(x => x.PosDeviceId)
            .HasColumnName("pos_device_id");

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("inet");

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_events_tenant_id_tenants");

        builder.HasOne<TillSession>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TillSessionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_events_till_session_id_till_sessions");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.EventByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_events_event_by_tenant_user_id_tenant_users");

        builder.HasOne<PosDevice>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.PosDeviceId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_session_events_pos_device_id_pos_devices");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_till_session_events_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_till_session_events_event_type", "event_type IN ('OPENED', 'CLOSED', 'PAUSED', 'RESUMED', 'CASH_IN', 'CASH_OUT', 'NOTE')");
            t.HasCheckConstraint("ck_till_session_events_amount", "amount IS NULL OR amount >= 0");
        });
    }
}
