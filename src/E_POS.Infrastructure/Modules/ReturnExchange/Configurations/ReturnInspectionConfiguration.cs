using E_POS.Domain.Modules.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ReturnExchange.Configurations;

public sealed class ReturnInspectionConfiguration : IEntityTypeConfiguration<ReturnInspection>
{
    public void Configure(EntityTypeBuilder<ReturnInspection> builder)
    {
        builder.ToTable("return_inspections");

        builder.HasKey(x => x.Id).HasName("pk_return_inspections");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(x => x.UpdatedAt);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.SalesReturnLineId)
            .HasColumnName("sales_return_line_id")
            .IsRequired();

        builder.Property(x => x.InspectedByTenantUserId)
            .HasColumnName("inspected_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.InventoryLocationId)
            .HasColumnName("inventory_location_id")
            .IsRequired(false);

        builder.Property(x => x.InspectionStatus)
            .HasColumnName("inspection_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ConditionCode)
            .HasColumnName("condition_code")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired(false);

        builder.Property(x => x.RestockDecision)
            .HasColumnName("restock_decision")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.RestockQuantity)
            .HasColumnName("restock_quantity")
            .HasPrecision(18, 4)
            .IsRequired(false);

        builder.Property(x => x.RejectQuantity)
            .HasColumnName("reject_quantity")
            .HasPrecision(18, 4)
            .IsRequired(false);

        builder.Property(x => x.InspectionNotes)
            .HasColumnName("inspection_notes")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.InspectedAt)
            .HasColumnName("inspected_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_inspections_80f12ba2");

        builder.HasOne<E_POS.Domain.Modules.ReturnExchange.Entities.SalesReturnLine>()
            .WithMany()
            .HasForeignKey(x => x.SalesReturnLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_inspections_a1781c39");

        builder.HasOne<E_POS.Domain.Modules.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.InspectedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_inspections_9faf0f8c");

        builder.HasOne<E_POS.Domain.Modules.Inventory.Entities.InventoryLocation>()
            .WithMany()
            .HasForeignKey(x => x.InventoryLocationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_inspections_8666fede");
        // </second-brain-constraints>
    }
}