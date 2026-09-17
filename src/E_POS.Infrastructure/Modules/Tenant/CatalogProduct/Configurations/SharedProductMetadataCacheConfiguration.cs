using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class SharedProductMetadataCacheConfiguration : IEntityTypeConfiguration<SharedProductMetadataCache>
{
    public void Configure(EntityTypeBuilder<SharedProductMetadataCache> builder)
    {
        builder.ToTable("shared_product_metadata_cache");

        builder.HasKey(x => x.Id).HasName("pk_shared_product_metadata_cache");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.NormalizedBarcode)
            .HasColumnName("normalized_barcode")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.IdentifierStandard)
            .HasColumnName("identifier_standard")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired(false);

        builder.Property(x => x.Provider)
            .HasColumnName("provider")
            .HasColumnType("varchar(60)")
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(x => x.NormalizedMetadataJson)
            .HasColumnName("normalized_metadata_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.RawResponseJson)
            .HasColumnName("raw_response_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.CachedAt)
            .HasColumnName("cached_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.LastVerifiedAt)
            .HasColumnName("last_verified_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

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

        // Unique constraint: one cache entry per barcode + provider
        builder.HasIndex(x => new { x.NormalizedBarcode, x.Provider })
            .IsUnique()
            .HasDatabaseName("uq_shared_product_metadata_cache_barcode_provider");

        // Index on normalized_barcode + expires_at for efficient lookup filtering
        builder.HasIndex(x => new { x.NormalizedBarcode, x.ExpiresAt })
            .HasDatabaseName("ix_shared_product_metadata_cache_barcode_expires");
    }
}
