using E_POS.Domain.Modules.OfflineSync.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.OfflineSync.Configurations;

public sealed class SyncItemConfiguration : IEntityTypeConfiguration<SyncItem>
{
    public void Configure(EntityTypeBuilder<SyncItem> builder)
    {
        builder.ToTable("sync_items");

        builder.HasKey(x => x.Id).HasName("pk_sync_items");

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

        builder.Property(x => x.OperationType)
            .HasColumnName("operation_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.ClientRecordId)
            .HasColumnName("client_record_id");

        builder.Property(x => x.EntityName)
            .HasColumnName("entity_name")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.PayloadHash)
            .HasColumnName("payload_hash")
            .HasColumnType("char(64)")
            .HasMaxLength(64);

        builder.Property(x => x.SyncBatchId)
            .HasColumnName("sync_batch_id")
            .IsRequired();

        builder.HasOne<SyncBatch>()
            .WithMany()
            .HasForeignKey(x => x.SyncBatchId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_items_sync_batch_id_sync_batches");

        builder.HasOne<OfflineClient>()
            .WithMany()
            .HasForeignKey(x => x.OfflineClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_items_offline_client_id_offline_clients");

        builder.HasIndex(x => new { x.TenantId, x.OfflineClientId, x.EntityName, x.ClientRecordId, x.OperationType, x.PayloadHash })
            .IsUnique()
            .HasDatabaseName("uq_sync_items_tenant_id_offline_client_id_entity_name_client_record_id_operation_type_payload_hash")
            .HasFilter("payload_hash IS NOT NULL");
    }
}

