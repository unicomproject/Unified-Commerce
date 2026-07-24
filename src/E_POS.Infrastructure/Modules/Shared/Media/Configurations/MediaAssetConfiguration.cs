using E_POS.Domain.Modules.Shared.Media.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.Media.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("media_assets");

        builder.HasKey(x => x.Id).HasName("pk_media_assets");

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

        builder.Property(x => x.ContainerName)
            .HasColumnName("container_name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.StorageKey)
            .HasColumnName("storage_key")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.PublicUrl)
            .HasColumnName("public_url")
            .HasColumnType("varchar(1000)")
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(x => x.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.MimeType)
            .HasColumnName("mime_type")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.FileExtension)
            .HasColumnName("file_extension")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.FileSizeBytes)
            .HasColumnName("file_size_bytes")
            .IsRequired();

        builder.Property(x => x.WidthPx)
            .HasColumnName("width_px")
            .IsRequired(false);

        builder.Property(x => x.HeightPx)
            .HasColumnName("height_px")
            .IsRequired(false);

        builder.Property(x => x.ChecksumHash)
            .HasColumnName("checksum_hash")
            .HasColumnType("varchar(128)")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.AssetType)
            .HasColumnName("asset_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.AssetPurpose)
            .HasColumnName("asset_purpose")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_media_assets_tenant_id_tenants");

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("ak_media_assets_tenant_id_id");

        builder.HasIndex(x => new { x.TenantId, x.ContainerName, x.StorageKey })
            .IsUnique()
            .HasDatabaseName("uq_media_assets_tenant_id_container_name_storage_key");

        builder.HasIndex(x => new { x.TenantId, x.AssetPurpose, x.Status })
            .HasDatabaseName("ix_media_assets_tenant_id_asset_purpose_status");

        builder.ToTable(t => t.HasCheckConstraint("ck_media_assets_file_size_bytes", "file_size_bytes > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_media_assets_width_px", "width_px IS NULL OR width_px > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_media_assets_height_px", "height_px IS NULL OR height_px > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_media_assets_asset_type", "asset_type IN ('IMAGE')"));
        builder.ToTable(t => t.HasCheckConstraint("ck_media_assets_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}
