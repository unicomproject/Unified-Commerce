using E_POS.Domain.Modules.Inventory.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Inventory.Configurations;

public sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("stock_adjustments");

        builder.HasKey(x => x.Id).HasName("pk_stock_adjustments");

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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.AdjustmentNumber)
            .HasColumnName("adjustment_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.AdjustmentStatus)
            .HasColumnName("adjustment_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_stock_adjustments_tenant_id_tenants");

        builder.HasIndex(x => new { x.TenantId, x.AdjustmentNumber })
            .IsUnique()
            .HasDatabaseName("uq_stock_adjustments_tenant_id_adjustment_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_stock_adjustments_adjustment_status", "adjustment_status IN ('DRAFT', 'APPROVED', 'POSTED', 'CANCELLED')")); 
    }
}

