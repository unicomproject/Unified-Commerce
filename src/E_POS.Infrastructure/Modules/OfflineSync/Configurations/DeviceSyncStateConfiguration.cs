using E_POS.Domain.Modules.OfflineSync.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.OfflineSync.Configurations;

public sealed class DeviceSyncStateConfiguration : IEntityTypeConfiguration<DeviceSyncState>
{
    public void Configure(EntityTypeBuilder<DeviceSyncState> builder)
    {
        builder.ToTable("device_sync_states");

        builder.HasKey(x => x.Id).HasName("pk_device_sync_states");

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

        builder.Property(x => x.OfflineClientId)
            .HasColumnName("offline_client_id")
            .IsRequired();

        builder.Property(x => x.DatasetName)
            .HasColumnName("dataset_name")
            .IsRequired();

        builder.Property(x => x.SyncDirection)
            .HasColumnName("sync_direction")
            .IsRequired();

        builder.Property(x => x.SyncFilterJson)
            .HasColumnName("sync_filter_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.LastFullSyncAt)
            .HasColumnName("last_full_sync_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.LastIncrementalSyncAt)
            .HasColumnName("last_incremental_sync_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.LastServerVersion)
            .HasColumnName("last_server_version")
            .IsRequired(false);

        builder.Property(x => x.LastClientVersion)
            .HasColumnName("last_client_version")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.OfflineClientId, x.DatasetName })
            .IsUnique()
            .HasDatabaseName("ux_device_sync_states_8060ebb9");

        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_device_sync_states_a0868895");

        builder.HasOne<E_POS.Domain.Modules.OfflineSync.Entities.OfflineClient>()
            .WithMany()
            .HasForeignKey(x => x.OfflineClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_device_sync_states_fe00dcdd");
        // <second-brain-checks>
        builder.ToTable(t => t.HasCheckConstraint("ck_device_sync_states_cb94ed2e", "last_server_version IS NULL OR last_server_version >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_device_sync_states_2920bfd8", "last_client_version IS NULL OR last_client_version >= 0"));
        // </second-brain-checks>

        // </second-brain-constraints>
    }
}