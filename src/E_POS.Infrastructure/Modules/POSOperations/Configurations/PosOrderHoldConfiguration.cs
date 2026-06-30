using E_POS.Domain.Modules.Orders.Entities;
using E_POS.Domain.Modules.POSOperations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.POSOperations.Configurations;

public sealed class PosOrderHoldConfiguration : IEntityTypeConfiguration<PosOrderHold>
{
    public void Configure(EntityTypeBuilder<PosOrderHold> builder)
    {
        builder.ToTable("pos_order_holds");

        builder.HasKey(x => x.Id).HasName("pk_pos_order_holds");

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

        builder.Property(x => x.HoldNumber)
            .HasColumnName("hold_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired(false);

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pos_order_holds_sales_order_id_sales_orders");

        builder.HasIndex(x => new { x.TenantId, x.HoldNumber })
            .IsUnique()
            .HasDatabaseName("uq_pos_order_holds_tenant_id_hold_number");
    }
}

