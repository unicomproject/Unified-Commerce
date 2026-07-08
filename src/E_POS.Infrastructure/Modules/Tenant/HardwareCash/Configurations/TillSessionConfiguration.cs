using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities; // for Currency and Tenant
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Configurations;

public sealed class TillSessionConfiguration : IEntityTypeConfiguration<TillSession>
{
    public void Configure(EntityTypeBuilder<TillSession> builder)
    {
        builder.ToTable("till_sessions");

        builder.HasKey(x => x.Id).HasName("pk_till_sessions");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.TillId).HasColumnName("till_id").IsRequired();
        builder.Property(x => x.SessionNumber).HasColumnName("session_number").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.BusinessDate).HasColumnName("business_date").HasColumnType("date").IsRequired();
        builder.Property(x => x.OpenedByTenantUserId).HasColumnName("opened_by_tenant_user_id").IsRequired();
        builder.Property(x => x.ClosedByTenantUserId).HasColumnName("closed_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.OpenedFromPosDeviceId).HasColumnName("opened_from_pos_device_id").IsRequired();
        builder.Property(x => x.ClosedFromPosDeviceId).HasColumnName("closed_from_pos_device_id").IsRequired(false);
        builder.Property(x => x.OpeningFloatAmount).HasColumnName("opening_float_amount").HasPrecision(18, 4).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasColumnType("char(3)").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.OpenedAt).HasColumnName("opened_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ClosedAt).HasColumnName("closed_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.OpeningNote).HasColumnName("opening_note").HasColumnType("text").IsRequired(false);
        builder.Property(x => x.ClosingNote).HasColumnName("closing_note").HasColumnType("text").IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_till_sessions_tenant_id_tenants");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => new { x.TenantId, x.OutletId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_till_sessions_outlet_id_outlets");
        builder.HasOne<Till>().WithMany().HasForeignKey(x => new { x.TenantId, x.TillId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_till_sessions_till_id_tills");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.OpenedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_till_sessions_opened_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.ClosedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_till_sessions_closed_by_tenant_user_id_tenant_users");
        
        builder.HasOne<PosDevice>().WithMany().HasForeignKey(x => new { x.TenantId, x.OpenedFromPosDeviceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_till_sessions_opened_from_pos_device_id_pos_devices");
        builder.HasOne<PosDevice>().WithMany().HasForeignKey(x => new { x.TenantId, x.ClosedFromPosDeviceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_till_sessions_closed_from_pos_device_id_pos_devices");
        
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).HasPrincipalKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_till_sessions_currency_code_currencies");

        builder.HasIndex(x => new { x.TenantId, x.SessionNumber }).IsUnique().HasDatabaseName("uq_till_sessions_tenant_id_session_number");
        builder.HasIndex(x => x.TillId).IsUnique().HasDatabaseName("uq_till_sessions_till_id_active").HasFilter("closed_at IS NULL");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_till_sessions_opening_float_amount", "opening_float_amount >= 0");
            t.HasCheckConstraint("ck_till_sessions_status", "status <> ''");
        });
    }
}



