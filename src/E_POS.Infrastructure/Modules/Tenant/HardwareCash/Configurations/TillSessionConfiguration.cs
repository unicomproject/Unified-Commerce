using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Configurations;

public sealed class TillSessionConfiguration : IEntityTypeConfiguration<TillSession>
{
    public void Configure(EntityTypeBuilder<TillSession> builder)
    {
        builder.ToTable("till_sessions");

        builder.HasKey(x => x.Id).HasName("pk_till_sessions");

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
            .HasColumnName("tenant_id");

        builder.Property(x => x.TillId)
            .HasColumnName("till_id")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ClosingCashAmount)
            .HasColumnName("closing_cash_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.OpenedByTenantUserId)
            .HasColumnName("opened_by_tenant_user_id")
            .IsRequired();

        builder.Property(x => x.SessionNumber)
            .HasColumnName("session_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.HasOne<Till>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TillId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_sessions_till_id_tills");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.OpenedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_sessions_opened_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.TillId, x.SessionNumber })
            .IsUnique()
            .HasDatabaseName("uq_till_sessions_tenant_id_till_id_session_number");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_till_sessions_tenant_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_till_sessions_closing_cash_amount", "closing_cash_amount IS NULL OR closing_cash_amount >= 0")); 
    }
}



