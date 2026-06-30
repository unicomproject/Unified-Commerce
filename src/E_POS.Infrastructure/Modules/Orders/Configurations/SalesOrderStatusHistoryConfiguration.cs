using E_POS.Domain.Modules.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Orders.Configurations;

public sealed class SalesOrderStatusHistoryConfiguration : IEntityTypeConfiguration<SalesOrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<SalesOrderStatusHistory> builder)
    {
        builder.ToTable("sales_order_status_history");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_status_history");

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

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired(false);

        builder.Property(x => x.SequenceNumber)
            .HasColumnName("sequence_number");

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_order_status_history_sales_order_id_sales_orders");

        builder.HasIndex(x => new { x.SalesOrderId, x.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("uq_sales_order_status_history_sales_order_id_sequence_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_order_status_history_sequence_number", "sequence_number > 0")); 
    }
}

