using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using TenantEntity = E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Configurations;

public sealed class TillActivationCodeConfiguration : IEntityTypeConfiguration<TillActivationCode>
{
    public void Configure(EntityTypeBuilder<TillActivationCode> builder)
    {
        builder.ToTable("till_activation_codes");
        builder.HasKey(x => x.Id).HasName("pk_till_activation_codes");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.TillId).HasColumnName("till_id").IsRequired();
        builder.Property(x => x.ActivationCodeHash)
            .HasColumnName("activation_code_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();
        builder.Property(x => x.IssuedByTenantUserId).HasColumnName("issued_by_tenant_user_id").IsRequired();
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(x => x.UsedByPosDeviceId).HasColumnName("used_by_pos_device_id").IsRequired(false);
        builder.Property(x => x.UsedAt)
            .HasColumnName("used_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<TenantEntity>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_activation_codes_tenant_id_tenants");

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => x.OutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_activation_codes_outlet_id_outlets");

        builder.HasOne<Till>()
            .WithMany()
            .HasForeignKey(x => x.TillId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_activation_codes_till_id_tills");

        builder.HasOne<PosDevice>()
            .WithMany()
            .HasForeignKey(x => x.UsedByPosDeviceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_activation_codes_used_by_pos_device_id_pos_devices");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.IssuedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_till_activation_codes_issued_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => x.ActivationCodeHash)
            .IsUnique()
            .HasDatabaseName("uq_till_activation_codes_activation_code_hash");

        builder.HasIndex(x => new { x.TenantId, x.TillId, x.Status })
            .HasDatabaseName("ix_till_activation_codes_tenant_id_till_id_status");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_till_activation_codes_status",
                "status IN ('ACTIVE', 'USED', 'EXPIRED', 'REVOKED')");
        });
    }
}
