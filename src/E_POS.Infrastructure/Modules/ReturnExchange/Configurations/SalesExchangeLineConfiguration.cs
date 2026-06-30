using E_POS.Domain.Modules.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ReturnExchange.Configurations;

public sealed class SalesExchangeLineConfiguration : IEntityTypeConfiguration<SalesExchangeLine>
{
    public void Configure(EntityTypeBuilder<SalesExchangeLine> builder)
    {
        builder.ToTable("sales_exchange_lines");

        builder.HasKey(x => x.Id).HasName("pk_sales_exchange_lines");

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

        builder.Property(x => x.ReplacementQuantity)
            .HasColumnName("replacement_quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.ReturnedQuantity)
            .HasColumnName("returned_quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.SalesExchangeId)
            .HasColumnName("sales_exchange_id")
            .IsRequired();

        builder.Property(x => x.SalesReturnLineId)
            .HasColumnName("sales_return_line_id")
            .IsRequired();

        builder.HasOne<SalesExchange>()
            .WithMany()
            .HasForeignKey(x => x.SalesExchangeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchange_lines_sales_exchange_id_sales_exchanges");

        builder.HasOne<SalesReturnLine>()
            .WithMany()
            .HasForeignKey(x => x.SalesReturnLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchange_lines_sales_return_line_id_sales_return_lines");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_exchange_lines_returned_quantity", "returned_quantity > 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_exchange_lines_replacement_quantity", "replacement_quantity >= 0")); 
    }
}

