using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Configurations;

public sealed class ProductSetupInitialTrackingConfiguration : IEntityTypeConfiguration<ProductSetupInitialTracking>
{
    public void Configure(EntityTypeBuilder<ProductSetupInitialTracking> builder)
    {
        builder.ToTable("product_setup_initial_tracking");

        builder.HasKey(x => x.Id).HasName("pk_product_setup_initial_tracking");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.InitialBatchNumber).HasColumnName("initial_batch_number").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.InitialExpiryDate).HasColumnName("initial_expiry_date").HasColumnType("date").IsRequired(false);
        builder.Property(x => x.InitialSerialNumber).HasColumnName("initial_serial_number").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired(false);
        builder.Property(x => x.AssignedProductVariantId).HasColumnName("assigned_product_variant_id").IsRequired(false);
        builder.Property(x => x.IncompatibleClearConfirmedAt).HasColumnName("incompatible_clear_confirmed_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.ConsumedAt).HasColumnName("consumed_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsRequired().HasDefaultValue(1L);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_setup_initial_tracking_tenant_id_tenants");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProductId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_product_setup_initial_tracking_product_id_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.AssignedProductVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_setup_initial_tracking_assigned_variant");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_setup_initial_tracking_created_by");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_setup_initial_tracking_updated_by");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_product_setup_initial_tracking_tenant_id_id");

        builder.HasIndex(x => new { x.TenantId, x.ProductId })
            .IsUnique()
            .HasDatabaseName("uq_product_setup_initial_tracking_tenant_id_product_id");

        builder.HasIndex(x => new { x.TenantId, x.ConsumedAt })
            .HasDatabaseName("ix_product_setup_initial_tracking_tenant_id_consumed_at")
            .HasFilter("consumed_at IS NULL");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_product_setup_initial_tracking_row_version", "row_version >= 1");
        });
    }
}
