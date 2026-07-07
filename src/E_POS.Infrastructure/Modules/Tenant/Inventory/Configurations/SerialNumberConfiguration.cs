using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Configurations;

public sealed class SerialNumberConfiguration : IEntityTypeConfiguration<SerialNumber>
{
    public void Configure(EntityTypeBuilder<SerialNumber> builder)
    {
        builder.ToTable("serial_numbers");

        builder.HasKey(x => x.Id).HasName("pk_serial_numbers");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id").IsRequired(false);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.ProductVariantId).HasColumnName("product_variant_id").IsRequired(false);
        builder.Property(x => x.ProductBatchId).HasColumnName("product_batch_id").IsRequired(false);
        builder.Property(x => x.CurrentInventoryBalanceId).HasColumnName("current_inventory_balance_id").IsRequired(false);
        builder.Property(x => x.SerialNumberValue).HasColumnName("serial_number").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.SerialStatus).HasColumnName("serial_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ReceivedAt).HasColumnName("received_at").HasColumnType("timestamp with time zone").IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_serial_numbers_tenant_id_tenants");
        
        builder.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_serial_numbers_product_id_products");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductVariantId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_serial_numbers_product_variant_id_product_variants");
        builder.HasOne<ProductBatch>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductBatchId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_serial_numbers_product_batch_id_product_batches");
        builder.HasOne<InventoryBalance>().WithMany().HasForeignKey(x => new { x.TenantId, x.CurrentInventoryBalanceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_serial_numbers_current_inventory_balance_id_inventory_balances");

        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_serial_numbers_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_serial_numbers_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.SerialNumberValue }).IsUnique().HasDatabaseName("uq_serial_numbers_tenant_id_product_id_serial_number");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_serial_numbers_tenant_id_id");
    }
}


