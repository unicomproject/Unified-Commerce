using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Configurations;

public sealed class TillCashMovementConfiguration : IEntityTypeConfiguration<TillCashMovement>
{
    public void Configure(EntityTypeBuilder<TillCashMovement> builder)
    {
        builder.ToTable("till_cash_movements");

        builder.HasKey(x => x.Id).HasName("pk_till_cash_movements");

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

        builder.Property(x => x.MovementType)
            .HasColumnName("movement_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250);

        builder.Property(x => x.ReferenceNumber)
            .HasColumnName("reference_number")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100);

        builder.Property(x => x.PerformedByTenantUserId)
            .HasColumnName("performed_by_tenant_user_id")
            .IsRequired();

        builder.Property(x => x.PerformedAt)
            .HasColumnName("performed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_cash_movements_tenant_id_tenants");

        builder.HasOne<TillSession>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TillSessionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_cash_movements_till_session_id_till_sessions");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.PerformedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_cash_movements_performed_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_till_cash_movements_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_till_cash_movements_movement_type", "movement_type IN ('CASH_IN', 'CASH_OUT', 'OPENING_FLOAT', 'CLOSING_REMOVE')");
            t.HasCheckConstraint("ck_till_cash_movements_amount", "amount > 0");
        });
    }
}



