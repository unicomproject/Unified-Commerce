using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Configurations;

public sealed class SalesExchangeEventConfiguration : IEntityTypeConfiguration<SalesExchangeEvent>
{
    public void Configure(EntityTypeBuilder<SalesExchangeEvent> builder)
    {
        builder.ToTable("sales_exchange_events");

        builder.HasKey(x => x.Id).HasName("pk_sales_exchange_events");

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

        builder.Property(x => x.SalesExchangeId)
            .HasColumnName("sales_exchange_id")
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.OldStatus)
            .HasColumnName("old_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(x => x.NewStatus)
            .HasColumnName("new_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(x => x.EventNotes)
            .HasColumnName("event_notes")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchange_events_f22e3e9c");

        builder.HasOne<E_POS.Domain.Modules.Shared.ReturnExchange.Entities.SalesExchange>()
            .WithMany()
            .HasForeignKey(x => x.SalesExchangeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchange_events_23d6ab17");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchange_events_9a6cae34");
        // </second-brain-constraints>
    }
}

