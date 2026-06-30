using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class InventoryCostLayerConfiguration : IEntityTypeConfiguration<InventoryCostLayer>
{
    public void Configure(EntityTypeBuilder<InventoryCostLayer> builder)
    {
        builder.ToTable("inventory_cost_layers");

        builder.HasKey(x => x.Id).HasName("pk_inventory_cost_layers");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.ProductBatchId)
            .HasColumnName("product_batch_id")
            .IsRequired(false);

        builder.Property(x => x.QuantityRemaining)
            .HasColumnName("quantity_remaining")
            .HasPrecision(18, 4);

        builder.Property(x => x.UnitCost)
            .HasColumnName("unit_cost")
            .HasPrecision(18, 2);

        builder.HasOne<ProductBatch>()
            .WithMany()
            .HasForeignKey(x => x.ProductBatchId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventory_cost_layers_product_batch_id_product_batches");

        builder.ToTable(t => t.HasCheckConstraint("ck_inventory_cost_layers_quantity_remaining", "quantity_remaining >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_inventory_cost_layers_unit_cost", "unit_cost >= 0")); 
    }
}

