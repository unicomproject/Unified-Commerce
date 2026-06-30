using E_POS.Domain.Modules.OfflineSync.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.OfflineSync.Configurations;

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

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ConflictCount)
            .HasColumnName("conflict_count");

        builder.Property(x => x.DownloadedItemCount)
            .HasColumnName("downloaded_item_count");

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120);

        builder.Property(x => x.UploadedItemCount)
            .HasColumnName("uploaded_item_count");

        builder.HasOne<OfflineClient>()
            .WithMany()
            .HasForeignKey(x => x.OfflineClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_batches_offline_client_id_offline_clients");

        builder.HasIndex(x => new { x.TenantId, x.OfflineClientId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("uq_sync_batches_tenant_id_offline_client_id_idempotency_key")
            .HasFilter("idempotency_key IS NOT NULL");

        builder.ToTable(t => t.HasCheckConstraint("ck_sync_batches_uploaded_item_count", "uploaded_item_count >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_sync_batches_downloaded_item_count", "downloaded_item_count >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_sync_batches_conflict_count", "conflict_count >= 0")); 
    }
}

