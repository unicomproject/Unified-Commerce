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
            .HasColumnName("tenant_id");

        builder.Property(x => x.OfflineClientId)
            .HasColumnName("offline_client_id")
            .IsRequired();

        builder.Property(x => x.DatasetName)
            .HasColumnName("dataset_name")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.LastClientVersion)
            .HasColumnName("last_client_version");

        builder.Property(x => x.LastServerVersion)
            .HasColumnName("last_server_version");

        builder.HasOne<OfflineClient>()
            .WithMany()
            .HasForeignKey(x => x.OfflineClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_device_sync_states_offline_client_id_offline_clients");

        builder.HasIndex(x => new { x.TenantId, x.OfflineClientId, x.DatasetName })
            .IsUnique()
            .HasDatabaseName("uq_device_sync_states_tenant_id_offline_client_id_dataset_name");

        builder.ToTable(t => t.HasCheckConstraint("ck_device_sync_states_last_server_version", "last_server_version IS NULL OR last_server_version >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_device_sync_states_last_client_version", "last_client_version IS NULL OR last_client_version >= 0")); 
    }
}

