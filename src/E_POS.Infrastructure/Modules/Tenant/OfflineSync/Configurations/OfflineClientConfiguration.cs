using E_POS.Domain.Modules.Tenant.OfflineSync.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OfflineSync.Configurations;

public sealed class OfflineClientConfiguration : IEntityTypeConfiguration<OfflineClient>
{
    public void Configure(EntityTypeBuilder<OfflineClient> builder)
    {
        builder.ToTable("offline_clients");

        builder.HasKey(x => x.Id).HasName("pk_offline_clients");

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

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id")
            .IsRequired();

        builder.Property(x => x.PosDeviceId)
            .HasColumnName("pos_device_id")
            .IsRequired();

        builder.Property(x => x.ClientCode)
            .HasColumnName("client_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.ClientName)
            .HasColumnName("client_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.OfflineType)
            .HasColumnName("offline_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.OfflineEnabled)
            .HasColumnName("offline_enabled")
            .IsRequired();

        builder.Property(x => x.MaxOfflineDurationMinutes)
            .HasColumnName("max_offline_duration_minutes")
            .IsRequired(false);

        builder.Property(x => x.ClientKeyHash)
            .HasColumnName("client_key_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(x => x.LastSeenAt)
            .HasColumnName("last_seen_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.LastSyncAt)
            .HasColumnName("last_sync_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.ClientCode })
            .IsUnique()
            .HasDatabaseName("ux_offline_clients_60137369");

        builder.HasIndex(x => new { x.TenantId, x.PosDeviceId })
            .IsUnique()
            .HasDatabaseName("ux_offline_clients_86310fa1");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_clients_501ce399");

        builder.HasOne<E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities.Outlet>()
            .WithMany()
            .HasForeignKey(x => x.OutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_clients_7adf2ed4");

        builder.HasOne<E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities.PosDevice>()
            .WithMany()
            .HasForeignKey(x => x.PosDeviceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_clients_d9af8e18");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_clients_c5114e92");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_clients_4165eaf4");
        // <second-brain-checks>
        builder.ToTable(t => t.HasCheckConstraint("ck_offline_clients_09d344aa", "max_offline_duration_minutes IS NULL OR max_offline_duration_minutes > 0"));
        // </second-brain-checks>

        // </second-brain-constraints>
    }
}

