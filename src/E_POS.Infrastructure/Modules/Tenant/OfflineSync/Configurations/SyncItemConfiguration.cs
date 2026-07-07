using E_POS.Domain.Modules.Tenant.OfflineSync.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OfflineSync.Configurations;

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

        builder.Ignore(x => x.UpdatedAt);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.SyncBatchId)
            .HasColumnName("sync_batch_id")
            .IsRequired();

        builder.Property(x => x.OfflineClientId)
            .HasColumnName("offline_client_id")
            .IsRequired();

        builder.Property(x => x.Direction)
            .HasColumnName("direction")
            .IsRequired();

        builder.Property(x => x.EntityName)
            .HasColumnName("entity_name")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.ClientRecordId)
            .HasColumnName("client_record_id")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(x => x.ServerRecordId)
            .HasColumnName("server_record_id")
            .IsRequired(false);

        builder.Property(x => x.OperationType)
            .HasColumnName("operation_type")
            .IsRequired();

        builder.Property(x => x.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.PayloadHash)
            .HasColumnName("payload_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(x => x.ItemStatus)
            .HasColumnName("item_status")
            .IsRequired();

        builder.Property(x => x.ErrorCode)
            .HasColumnName("error_code")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.ReceivedAt)
            .HasColumnName("received_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.OfflineClientId, x.EntityName, x.ClientRecordId, x.OperationType, x.PayloadHash })
            .IsUnique()
            .HasDatabaseName("ux_sync_items_ae7f345a");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_items_23643150");

        builder.HasOne<E_POS.Domain.Modules.Tenant.OfflineSync.Entities.SyncBatch>()
            .WithMany()
            .HasForeignKey(x => x.SyncBatchId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_items_e950bc88");

        builder.HasOne<E_POS.Domain.Modules.Tenant.OfflineSync.Entities.OfflineClient>()
            .WithMany()
            .HasForeignKey(x => x.OfflineClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_items_c57862d6");
        // </second-brain-constraints>
    }
}

