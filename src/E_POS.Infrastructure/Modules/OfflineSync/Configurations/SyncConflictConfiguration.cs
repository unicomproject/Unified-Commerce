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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.OfflineClientId)
            .HasColumnName("offline_client_id")
            .IsRequired();

        builder.Property(x => x.SyncBatchId)
            .HasColumnName("sync_batch_id")
            .IsRequired();

        builder.Property(x => x.SyncItemId)
            .HasColumnName("sync_item_id")
            .IsRequired(false);

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

        builder.Property(x => x.ConflictType)
            .HasColumnName("conflict_type")
            .IsRequired();

        builder.Property(x => x.ClientPayloadJson)
            .HasColumnName("client_payload_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.ServerPayloadJson)
            .HasColumnName("server_payload_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.ResolutionStatus)
            .HasColumnName("resolution_status")
            .IsRequired();

        builder.Property(x => x.ResolutionStrategy)
            .HasColumnName("resolution_strategy")
            .IsRequired(false);

        builder.Property(x => x.ResolutionNote)
            .HasColumnName("resolution_note")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.ResolvedByTenantUserId)
            .HasColumnName("resolved_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.ResolvedAt)
            .HasColumnName("resolved_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_conflicts_cbd64441");

        builder.HasOne<E_POS.Domain.Modules.OfflineSync.Entities.OfflineClient>()
            .WithMany()
            .HasForeignKey(x => x.OfflineClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_conflicts_6b3e3426");

        builder.HasOne<E_POS.Domain.Modules.OfflineSync.Entities.SyncBatch>()
            .WithMany()
            .HasForeignKey(x => x.SyncBatchId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_conflicts_ca8c9281");

        builder.HasOne<E_POS.Domain.Modules.OfflineSync.Entities.SyncItem>()
            .WithMany()
            .HasForeignKey(x => x.SyncItemId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_conflicts_8f32d9fc");

        builder.HasOne<E_POS.Domain.Modules.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.ResolvedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sync_conflicts_c8d8863b");
        // </second-brain-constraints>
    }
}