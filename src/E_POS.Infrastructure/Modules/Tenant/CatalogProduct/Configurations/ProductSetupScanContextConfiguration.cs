using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductSetupScanContextConfiguration : IEntityTypeConfiguration<ProductSetupScanContext>
{
    public void Configure(EntityTypeBuilder<ProductSetupScanContext> builder)
    {
        builder.ToTable("product_setup_scan_context");

        builder.HasKey(x => x.Id).HasName("pk_product_setup_scan_context");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.AcquisitionMode).HasColumnName("acquisition_mode").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CandidateIdentifier).HasColumnName("candidate_identifier").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.IdentifierStandard).HasColumnName("identifier_standard").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired(false);
        builder.Property(x => x.SymbologyHint).HasColumnName("symbology_hint").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired(false);
        builder.Property(x => x.NoBarcodeReason).HasColumnName("no_barcode_reason").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired(false);
        builder.Property(x => x.ExternalLookupStatus).HasColumnName("external_lookup_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired(false);
        builder.Property(x => x.ExternalSourceReference).HasColumnName("external_source_reference").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.NormalizedPrefillJson).HasColumnName("normalized_prefill_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(x => x.GeneratedSkuCandidate).HasColumnName("generated_sku_candidate").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsRequired().HasDefaultValue(1L);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_setup_scan_context_tenant_id_tenants");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_product_setup_scan_context_product_id_products");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_setup_scan_context_created_by");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_setup_scan_context_updated_by");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_product_setup_scan_context_tenant_id_id");

        builder.HasIndex(x => new { x.TenantId, x.ProductId })
            .IsUnique()
            .HasDatabaseName("uq_product_setup_scan_context_tenant_id_product_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_product_setup_scan_context_acquisition_mode",
                "acquisition_mode IN ('SCAN', 'MANUAL', 'NO_BARCODE', 'LEGACY')");
            t.HasCheckConstraint(
                "ck_product_setup_scan_context_no_barcode_reason",
                "no_barcode_reason IS NULL OR no_barcode_reason IN ('OWN_MADE', 'SERVICE_FEE', 'UNLABELLED')");
            t.HasCheckConstraint("ck_product_setup_scan_context_row_version", "row_version >= 1");
        });
    }
}
