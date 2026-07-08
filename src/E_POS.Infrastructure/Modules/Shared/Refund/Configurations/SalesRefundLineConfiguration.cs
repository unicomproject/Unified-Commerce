using E_POS.Domain.Modules.Shared.Refund.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.Refund.Configurations;

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

        builder.Ignore(x => x.UpdatedAt);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.SalesRefundId)
            .HasColumnName("sales_refund_id")
            .IsRequired();

        builder.Property(x => x.SalesReturnLineId)
            .HasColumnName("sales_return_line_id")
            .IsRequired(false);

        builder.Property(x => x.RefundLineType)
            .HasColumnName("refund_line_type")
            .IsRequired();

        builder.Property(x => x.DescriptionSnapshot)
            .HasColumnName("description_snapshot")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4)
            .IsRequired(false);

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 4)
            .IsRequired();

        // <second-brain-constraints>
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refund_lines_66acc628");

        builder.HasOne<E_POS.Domain.Modules.Shared.Refund.Entities.SalesRefund>()
            .WithMany()
            .HasForeignKey(x => x.SalesRefundId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refund_lines_9ae29b0c");

        builder.HasOne<E_POS.Domain.Modules.Shared.ReturnExchange.Entities.SalesReturnLine>()
            .WithMany()
            .HasForeignKey(x => x.SalesReturnLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_refund_lines_b8e16b29");
        // <second-brain-checks>
        builder.ToTable(t => t.HasCheckConstraint("ck_sales_refund_lines_f90ed063", "quantity IS NULL OR quantity > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_sales_refund_lines_ef7f4598", "amount > 0"));
        // </second-brain-checks>

        // </second-brain-constraints>
    }
}

