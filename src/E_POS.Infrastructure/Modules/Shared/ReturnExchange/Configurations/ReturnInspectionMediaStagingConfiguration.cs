using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Configurations;

public sealed class ReturnInspectionMediaStagingConfiguration
    : IEntityTypeConfiguration<ReturnInspectionMediaStaging>
{
    public void Configure(EntityTypeBuilder<ReturnInspectionMediaStaging> builder)
    {
        builder.ToTable("return_inspection_media_staging", t =>
        {
            t.HasCheckConstraint(
                "ck_return_inspection_media_staging_status",
                "status IN ('STAGED', 'CONSUMED', 'EXPIRED', 'DELETED')");
            t.HasCheckConstraint(
                "ck_return_inspection_media_staging_size_bytes",
                "size_bytes > 0 AND size_bytes <= 5242880");
            t.HasCheckConstraint(
                "ck_return_inspection_media_staging_expires_at",
                "expires_at > created_at");
        });

        builder.HasKey(x => x.Id).HasName("pk_return_inspection_media_staging");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id");
        builder.Property(x => x.SaleId).HasColumnName("sale_id").IsRequired();
        builder.Property(x => x.SaleLineId).HasColumnName("sale_line_id").IsRequired();
        builder.Property(x => x.InspectionDraftId).HasColumnName("inspection_draft_id");
        builder.Property(x => x.InspectionDraftLineId).HasColumnName("inspection_draft_line_id");
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ConsumedAt).HasColumnName("consumed_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.StorageKey)
            .HasColumnName("storage_key")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(x => x.FileName)
            .HasColumnName("file_name")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();
        builder.Property(x => x.ContentType)
            .HasColumnName("content_type")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(x => x.SizeBytes).HasColumnName("size_bytes").IsRequired();
        builder.Property(x => x.UploadedByTenantUserId)
            .HasColumnName("uploaded_by_tenant_user_id")
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.SaleId, x.SaleLineId })
            .HasDatabaseName("ix_return_inspection_media_staging_sale_line");
        builder.HasIndex(x => new { x.Status, x.ExpiresAt })
            .HasDatabaseName("ix_return_inspection_media_staging_expiry");
        builder.HasIndex(x => x.StorageKey)
            .IsUnique()
            .HasDatabaseName("ux_return_inspection_media_staging_storage_key");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_inspection_media_staging_tenant");
    }
}
