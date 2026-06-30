using E_POS.Domain.Modules.OfflineSync.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.OfflineSync.Configurations;

public sealed class OfflineIdMappingConfiguration : IEntityTypeConfiguration<OfflineIdMapping>
{
    public void Configure(EntityTypeBuilder<OfflineIdMapping> builder)
    {
        builder.ToTable("offline_id_mappings");

        builder.HasKey(x => x.Id).HasName("pk_offline_id_mappings");

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

        builder.Property(x => x.ClientRecordId)
            .HasColumnName("client_record_id");

        builder.Property(x => x.CreatedFromSyncItemId)
            .HasColumnName("created_from_sync_item_id")
            .IsRequired();

        builder.Property(x => x.EntityName)
            .HasColumnName("entity_name")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.ServerRecordId)
            .HasColumnName("server_record_id");

        builder.HasOne<OfflineClient>()
            .WithMany()
            .HasForeignKey(x => x.OfflineClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_id_mappings_offline_client_id_offline_clients");

        builder.HasOne<SyncItem>()
            .WithMany()
            .HasForeignKey(x => x.CreatedFromSyncItemId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_id_mappings_created_from_sync_item_id_sync_items");

        builder.HasIndex(x => new { x.TenantId, x.OfflineClientId, x.EntityName, x.ClientRecordId })
            .IsUnique()
            .HasDatabaseName("uq_offline_id_mappings_tenant_id_offline_client_id_entity_name_client_record_id");

        builder.HasIndex(x => new { x.TenantId, x.EntityName, x.ServerRecordId })
            .IsUnique()
            .HasDatabaseName("uq_offline_id_mappings_tenant_id_entity_name_server_record_id");
    }
}

