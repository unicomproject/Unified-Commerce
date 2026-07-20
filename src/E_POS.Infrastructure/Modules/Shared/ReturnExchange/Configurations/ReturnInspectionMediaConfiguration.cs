using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Configurations;

public sealed class ReturnInspectionMediaConfiguration : IEntityTypeConfiguration<ReturnInspectionMedia>
{
    public void Configure(EntityTypeBuilder<ReturnInspectionMedia> builder)
    {
        builder.ToTable("return_inspection_media");
        builder.HasKey(x => x.Id).HasName("pk_return_inspection_media");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Ignore(x => x.UpdatedAt); builder.Ignore(x => x.CreatedBy); builder.Ignore(x => x.UpdatedBy);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ReturnInspectionId).HasColumnName("return_inspection_id").IsRequired();
        builder.Property(x => x.StorageKey).HasColumnName("storage_key").HasColumnType("varchar(500)").HasMaxLength(500).IsRequired();
        builder.Property(x => x.FileName).HasColumnName("file_name").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasColumnName("content_type").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(x => x.SizeBytes).HasColumnName("size_bytes").IsRequired();
        builder.Property(x => x.UploadedByTenantUserId).HasColumnName("uploaded_by_tenant_user_id").IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.ReturnInspectionId }).HasDatabaseName("ix_return_inspection_media_inspection");
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
