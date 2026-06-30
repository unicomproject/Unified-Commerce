using E_POS.Domain.Modules.Refund.Entities;
using E_POS.Domain.Modules.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Refund.Configurations;

public sealed class SalesRefundLineConfiguration : IEntityTypeConfiguration<SalesRefundLine>
{
    public void Configure(EntityTypeBuilder<SalesRefundLine> builder)
    {
        builder.ToTable("sales_refund_lines");

        builder.HasKey(x => x.Id).HasName("pk_sales_refund_lines");

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

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.SalesRefundId)
            .HasColumnName("sales_refund_id")
            .IsRequired();

        builder.Property(x => x.SalesReturnLineId)
            .HasColumnName("sales_return_line_id")
            .IsRequired();

        builder.HasOne<SalesRefund>()
            .WithMany()
            .HasForeignKey(x => x.SalesRefundId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refund_lines_sales_refund_id_sales_refunds");

        builder.HasOne<SalesReturnLine>()
            .WithMany()
            .HasForeignKey(x => x.SalesReturnLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refund_lines_sales_return_line_id_sales_return_lines");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_refund_lines_amount", "amount >= 0")); 
    }
}

