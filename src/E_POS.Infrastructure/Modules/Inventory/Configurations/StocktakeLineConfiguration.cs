using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class StocktakeLineConfiguration : IEntityTypeConfiguration<StocktakeLine>
{
    public void Configure(EntityTypeBuilder<StocktakeLine> builder)
    {
        builder.ToTable("stocktake_lines");

        builder.HasKey(x => x.Id).HasName("pk_stocktake_lines");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.StocktakeSessionId).HasColumnName("stocktake_session_id").IsRequired();
        builder.Property(x => x.LineNumber).HasColumnName("line_number").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.ProductVariantId).HasColumnName("product_variant_id").IsRequired(false);
        builder.Property(x => x.ProductBatchId).HasColumnName("product_batch_id").IsRequired(false);
        builder.Property(x => x.ExpectedQuantity).HasColumnName("expected_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.CountedQuantity).HasColumnName("counted_quantity").HasPrecision(18, 4).IsRequired(false);
        builder.Property(x => x.VarianceQuantity).HasColumnName("variance_quantity").HasPrecision(18, 4).HasComputedColumnSql("counted_quantity - expected_quantity", stored: true).IsRequired(false);
        builder.Property(x => x.CountedByTenantUserId).HasColumnName("counted_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.CountedAt).HasColumnName("counted_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.LineStatus).HasColumnName("line_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.LineNote).HasColumnName("line_note").HasColumnType("text").IsRequired(false);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_lines_tenant_id_tenants");
        
        builder.HasOne<StocktakeSession>().WithMany().HasForeignKey(x => new { x.TenantId, x.StocktakeSessionId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_lines_stocktake_session_id_stocktake_sessions");
        builder.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_lines_product_id_products");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductVariantId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_lines_product_variant_id_product_variants");
        builder.HasOne<ProductBatch>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductBatchId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_lines_product_batch_id_product_batches");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CountedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stocktake_lines_counted_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.StocktakeSessionId, x.LineNumber }).IsUnique().HasDatabaseName("uq_stocktake_lines_tenant_id_stocktake_session_id_line_number");
        builder.HasIndex(x => new { x.TenantId, x.StocktakeSessionId, x.ProductId, x.ProductVariantId, x.ProductBatchId }).IsUnique().HasDatabaseName("uq_stocktake_lines_scope").AreNullsDistinct(false);
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_stocktake_lines_tenant_id_id");
    }
}