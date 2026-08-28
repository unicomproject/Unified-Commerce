using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.FulfilmentPickup.Configurations;

public sealed class FulfillmentPackageLineConfiguration : IEntityTypeConfiguration<FulfillmentPackageLine>
{
    public void Configure(EntityTypeBuilder<FulfillmentPackageLine> builder)
    {
        builder.ToTable("fulfillment_package_lines", t => t.HasCheckConstraint("ck_fulfillment_package_lines_quantity", "quantity > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.FulfillmentPackageId).HasColumnName("fulfillment_package_id").IsRequired();
        builder.Property(x => x.FulfillmentOrderLineId).HasColumnName("fulfillment_order_line_id").IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Ignore(x => x.CreatedBy); builder.Ignore(x => x.UpdatedBy);
        builder.HasIndex(x => new { x.TenantId, x.FulfillmentPackageId, x.FulfillmentOrderLineId }).IsUnique();
        builder.HasOne<FulfillmentPackage>().WithMany().HasForeignKey(x => x.FulfillmentPackageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FulfillmentOrderLine>().WithMany().HasForeignKey(x => x.FulfillmentOrderLineId).OnDelete(DeleteBehavior.Restrict);
    }
}
