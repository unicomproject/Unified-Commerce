using E_POS.Domain.Modules.OfflineSync.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.OfflineSync.Configurations;

public sealed class SyncConflictConfiguration : IEntityTypeConfiguration<SyncConflict>
{
    public void Configure(EntityTypeBuilder<SyncConflict> builder)
    {
        builder.ToTable("sync_conflicts");

        builder.HasKey(x => x.Id).HasName("pk_sync_conflicts");

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

        builder.Property(x => x.OfflineClientId)
            .HasColumnName("offline_client_id")
            .IsRequired();

        builder.Property(x => x.ResolutionStatus)
            .HasColumnName("resolution_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.SyncBatchId)
            .HasColumnName("sync_batch_id")
            .IsRequired();

        builder.Property(x => x.SyncItemId)
            .HasColumnName("sync_item_id")
            .IsRequired();

        builder.HasOne<OfflineClient>()
            .WithMany()
            .HasForeignKey(x => x.OfflineClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_conflicts_offline_client_id_offline_clients");

        builder.HasOne<SyncBatch>()
            .WithMany()
            .HasForeignKey(x => x.SyncBatchId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_conflicts_sync_batch_id_sync_batches");

        builder.HasOne<SyncItem>()
            .WithMany()
            .HasForeignKey(x => x.SyncItemId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_conflicts_sync_item_id_sync_items");

        builder.ToTable(t => t.HasCheckConstraint("ck_sync_conflicts_resolution_status", "resolution_status IN ('OPEN', 'RESOLVED', 'IGNORED', 'FAILED')")); 
    }
}

