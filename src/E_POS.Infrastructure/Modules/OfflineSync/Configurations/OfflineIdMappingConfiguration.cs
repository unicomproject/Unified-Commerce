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

        builder.Ignore(x => x.UpdatedAt);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.OfflineClientId)
            .HasColumnName("offline_client_id")
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
            .IsRequired();

        builder.Property(x => x.ServerRecordId)
            .HasColumnName("server_record_id")
            .IsRequired();

        builder.Property(x => x.CreatedFromSyncItemId)
            .HasColumnName("created_from_sync_item_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.OfflineClientId, x.EntityName, x.ClientRecordId })
            .IsUnique()
            .HasDatabaseName("ux_offline_id_mappings_34294dee");

        builder.HasIndex(x => new { x.TenantId, x.EntityName, x.ServerRecordId })
            .IsUnique()
            .HasDatabaseName("ux_offline_id_mappings_e7802dc9");

        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_id_mappings_e181ae14");

        builder.HasOne<E_POS.Domain.Modules.OfflineSync.Entities.OfflineClient>()
            .WithMany()
            .HasForeignKey(x => x.OfflineClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_id_mappings_6b4b0a22");

        builder.HasOne<E_POS.Domain.Modules.OfflineSync.Entities.SyncItem>()
            .WithMany()
            .HasForeignKey(x => x.CreatedFromSyncItemId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_id_mappings_8d80eb03");
        // </second-brain-constraints>
    }
}