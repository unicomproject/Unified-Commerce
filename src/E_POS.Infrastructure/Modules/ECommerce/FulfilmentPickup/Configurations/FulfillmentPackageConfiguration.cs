using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.FulfilmentPickup.Configurations;

public sealed class FulfillmentPackageConfiguration : IEntityTypeConfiguration<FulfillmentPackage>
{
    public void Configure(EntityTypeBuilder<FulfillmentPackage> builder)
    {
        builder.ToTable("fulfillment_packages", t => t.HasCheckConstraint(
            "ck_fulfillment_packages_status", "package_status IN ('OPEN','PACKED','READY','HANDED_OVER','CANCELLED')"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.FulfillmentOrderId).HasColumnName("fulfillment_order_id").IsRequired();
        builder.Property(x => x.PackageNumber).HasColumnName("package_number").HasMaxLength(80).IsRequired();
        builder.Property(x => x.StagingInventoryLocationId).HasColumnName("staging_inventory_location_id");
        builder.Property(x => x.PackageStatus).HasColumnName("package_status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.PackedByTenantUserId).HasColumnName("packed_by_tenant_user_id").IsRequired();
        builder.Property(x => x.PackedAt).HasColumnName("packed_at").IsRequired();
        builder.Property(x => x.ReadyAt).HasColumnName("ready_at");
        builder.Property(x => x.PackingNote).HasColumnName("packing_note").HasMaxLength(200);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsConcurrencyToken().IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Ignore(x => x.CreatedBy); builder.Ignore(x => x.UpdatedBy);
        builder.HasIndex(x => new { x.TenantId, x.FulfillmentOrderId, x.PackageNumber }).IsUnique();
        builder.HasOne<FulfillmentOrder>().WithMany().HasForeignKey(x => x.FulfillmentOrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
