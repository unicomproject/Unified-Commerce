using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.TenantAuth.Configurations;

public sealed class TenantAuthSessionConfiguration : IEntityTypeConfiguration<TenantAuthSession>
{
    public void Configure(EntityTypeBuilder<TenantAuthSession> builder)
    {
        builder.ToTable("tenant_auth_sessions");

        builder.HasKey(x => x.Id).HasName("pk_tenant_auth_sessions");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasColumnType("varchar(45)");
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasColumnType("text");
        builder.Property(x => x.DeviceName).HasColumnName("device_name").HasColumnType("varchar(150)").HasMaxLength(150);
        builder.Property(x => x.PosDeviceId).HasColumnName("pos_device_id");
        builder.Property(x => x.LastSeenAt).HasColumnName("last_seen_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.RevokedByTenantUserId).HasColumnName("revoked_by_tenant_user_id");
        builder.Property(x => x.RevokedByPlatformUserId).HasColumnName("revoked_by_platform_user_id");
        builder.Property(x => x.RevokeReason).HasColumnName("revoke_reason").HasColumnType("varchar(250)").HasMaxLength(250);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_auth_sessions_tenant_id_tenants");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_auth_sessions_user_id_tenant_users");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.RevokedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_auth_sessions_revoked_by_tenant_user_id_tenant_users");

        builder.ToTable(t => t.HasCheckConstraint("ck_tenant_auth_sessions_revoked_by", "NOT (revoked_by_tenant_user_id IS NOT NULL AND revoked_by_platform_user_id IS NOT NULL)"));
    }
}



