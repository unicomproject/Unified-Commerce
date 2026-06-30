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

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(x => x.InspectedQuantity)
            .HasColumnName("inspected_quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.InspectionNumber)
            .HasColumnName("inspection_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.SalesReturnLineId)
            .HasColumnName("sales_return_line_id")
            .IsRequired();

        builder.HasOne<SalesReturnLine>()
            .WithMany()
            .HasForeignKey(x => x.SalesReturnLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_inspections_sales_return_line_id_sales_return_lines");

        builder.HasIndex(x => new { x.TenantId, x.InspectionNumber })
            .IsUnique()
            .HasDatabaseName("uq_return_inspections_tenant_id_inspection_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_return_inspections_inspected_quantity", "inspected_quantity >= 0")); 
    }
}

