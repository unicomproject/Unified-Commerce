using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Configurations;

public sealed class PlatformTenantBootstrapProductImportBatchConfiguration
    : IEntityTypeConfiguration<PlatformTenantBootstrapProductImportBatch>
{
    public void Configure(EntityTypeBuilder<PlatformTenantBootstrapProductImportBatch> builder)
    {
        builder.ToTable("platform_tenant_bootstrap_product_import_batches");
        builder.HasKey(x => x.Id).HasName("pk_platform_tenant_bootstrap_product_import_batches");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(x => x.TemplateVersion).HasColumnName("template_version").HasMaxLength(64).IsRequired();
        builder.Property(x => x.SourceFileName).HasColumnName("source_file_name").HasMaxLength(260).IsRequired();
        builder.Property(x => x.TotalRows).HasColumnName("total_rows").IsRequired();
        builder.Property(x => x.ValidRows).HasColumnName("valid_rows").IsRequired();
        builder.Property(x => x.InvalidRows).HasColumnName("invalid_rows").IsRequired();
        builder.Property(x => x.CommittedRows).HasColumnName("committed_rows").IsRequired();
        builder.Property(x => x.SkippedRows).HasColumnName("skipped_rows").IsRequired();
        builder.Property(x => x.IdempotencyKeyHash).HasColumnName("idempotency_key_hash").HasMaxLength(64);
        builder.Property(x => x.ActorPlatformUserId).HasColumnName("actor_platform_user_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.HasIndex(x => new { x.TenantId, x.Status }).HasDatabaseName("ix_bootstrap_import_batches_tenant_status");
    }
}

public sealed class PlatformTenantBootstrapProductImportRowConfiguration
    : IEntityTypeConfiguration<PlatformTenantBootstrapProductImportRow>
{
    public void Configure(EntityTypeBuilder<PlatformTenantBootstrapProductImportRow> builder)
    {
        builder.ToTable("platform_tenant_bootstrap_product_import_rows");
        builder.HasKey(x => x.Id).HasName("pk_platform_tenant_bootstrap_product_import_rows");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ImportBatchId).HasColumnName("import_batch_id").IsRequired();
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.RowNumber).HasColumnName("row_number").IsRequired();
        builder.Property(x => x.RawRowJson).HasColumnName("raw_row_json").HasColumnType("text").IsRequired();
        builder.Property(x => x.IsValid).HasColumnName("is_valid").IsRequired();
        builder.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(100);
        builder.Property(x => x.ErrorDetail).HasColumnName("error_detail").HasMaxLength(500);
        builder.Property(x => x.CommittedProductId).HasColumnName("committed_product_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.HasIndex(x => new { x.ImportBatchId, x.RowNumber })
            .IsUnique()
            .HasDatabaseName("uq_bootstrap_import_rows_batch_row");
        builder.HasOne<PlatformTenantBootstrapProductImportBatch>()
            .WithMany()
            .HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_bootstrap_import_rows_batch");
    }
}

public sealed class PlatformTenantBootstrapIdempotencyRecordConfiguration
    : IEntityTypeConfiguration<PlatformTenantBootstrapIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<PlatformTenantBootstrapIdempotencyRecord> builder)
    {
        builder.ToTable("platform_tenant_bootstrap_idempotency_records");
        builder.HasKey(x => x.Id).HasName("pk_platform_tenant_bootstrap_idempotency_records");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OperationType).HasColumnName("operation_type").HasMaxLength(80).IsRequired();
        builder.Property(x => x.IdempotencyKeyHash).HasColumnName("idempotency_key_hash").HasMaxLength(64).IsRequired();
        builder.Property(x => x.RequestHash).HasColumnName("request_hash").HasMaxLength(64);
        builder.Property(x => x.ResponseJson).HasColumnName("response_json").HasColumnType("text").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.HasIndex(x => new { x.TenantId, x.OperationType, x.IdempotencyKeyHash })
            .IsUnique()
            .HasDatabaseName("uq_bootstrap_idempotency_tenant_operation_key");
    }
}
