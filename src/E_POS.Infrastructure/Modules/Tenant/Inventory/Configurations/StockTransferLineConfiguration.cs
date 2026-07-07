using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Configurations;

public sealed class StockTransferLineConfiguration : IEntityTypeConfiguration<StockTransferLine>
{
    public void Configure(EntityTypeBuilder<StockTransferLine> builder)
    {
        builder.ToTable("stock_transfer_lines");

        builder.HasKey(x => x.Id).HasName("pk_stock_transfer_lines");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.StockTransferId).HasColumnName("stock_transfer_id").IsRequired();
        builder.Property(x => x.LineNumber).HasColumnName("line_number").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.ProductVariantId).HasColumnName("product_variant_id").IsRequired(false);
        builder.Property(x => x.ProductBatchId).HasColumnName("product_batch_id").IsRequired(false);
        builder.Property(x => x.RequestedQuantity).HasColumnName("requested_quantity").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ShippedQuantity).HasColumnName("shipped_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.ReceivedQuantity).HasColumnName("received_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.DamagedQuantity).HasColumnName("damaged_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.MissingQuantity).HasColumnName("missing_quantity").HasPrecision(18, 4).HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.LineStatus).HasColumnName("line_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.LineNote).HasColumnName("line_note").HasColumnType("text").IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfer_lines_tenant_id_tenants");
        
        builder.HasOne<StockTransfer>().WithMany().HasForeignKey(x => new { x.TenantId, x.StockTransferId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfer_lines_stock_transfer_id_stock_transfers");
        builder.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfer_lines_product_id_products");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductVariantId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfer_lines_product_variant_id_product_variants");
        builder.HasOne<ProductBatch>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductBatchId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_stock_transfer_lines_product_batch_id_product_batches");

        builder.HasIndex(x => new { x.TenantId, x.StockTransferId, x.LineNumber }).IsUnique().HasDatabaseName("uq_stock_transfer_lines_tenant_id_stock_transfer_id_line_number");
        builder.HasIndex(x => new { x.TenantId, x.StockTransferId, x.ProductId, x.ProductVariantId, x.ProductBatchId }).IsUnique().HasDatabaseName("uq_stock_transfer_lines_scope").AreNullsDistinct(false);
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_stock_transfer_lines_tenant_id_id");
    }
}


