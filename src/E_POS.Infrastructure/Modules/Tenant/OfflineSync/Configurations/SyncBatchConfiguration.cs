using E_POS.Domain.Modules.Tenant.OfflineSync.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OfflineSync.Configurations;

public sealed class SyncBatchConfiguration : IEntityTypeConfiguration<SyncBatch>
{
    public void Configure(EntityTypeBuilder<SyncBatch> builder)
    {
        builder.ToTable("sync_batches");

        builder.HasKey(x => x.Id).HasName("pk_sync_batches");

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

        builder.Property(x => x.OfflineClientId)
            .HasColumnName("offline_client_id")
            .IsRequired();

        builder.Property(x => x.SyncType)
            .HasColumnName("sync_type")
            .IsRequired();

        builder.Property(x => x.SyncStatus)
            .HasColumnName("sync_status")
            .IsRequired();

        builder.Property(x => x.ClientStartedAt)
            .HasColumnName("client_started_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ServerStartedAt)
            .HasColumnName("server_started_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.FailedAt)
            .HasColumnName("failed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.FailureReason)
            .HasColumnName("failure_reason")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.UploadedItemCount)
            .HasColumnName("uploaded_item_count")
            .IsRequired();

        builder.Property(x => x.DownloadedItemCount)
            .HasColumnName("downloaded_item_count")
            .IsRequired();

        builder.Property(x => x.ConflictCount)
            .HasColumnName("conflict_count")
            .IsRequired();

        builder.Property(x => x.ClientAppVersion)
            .HasColumnName("client_app_version")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired(false);

        builder.Property(x => x.ClientLocalTime)
            .HasColumnName("client_local_time")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.OfflineClientId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_sync_batches_a99603d8");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_batches_e699358e");

        builder.HasOne<E_POS.Domain.Modules.Tenant.OfflineSync.Entities.OfflineClient>()
            .WithMany()
            .HasForeignKey(x => x.OfflineClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_batches_28c7db3d");
        // <second-brain-checks>
        builder.ToTable(t => t.HasCheckConstraint("ck_sync_batches_b689bf2f", "uploaded_item_count >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_sync_batches_fde4f4bc", "downloaded_item_count >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_sync_batches_615b7ce4", "conflict_count >= 0"));
        // </second-brain-checks>

        // </second-brain-constraints>
    }
}

