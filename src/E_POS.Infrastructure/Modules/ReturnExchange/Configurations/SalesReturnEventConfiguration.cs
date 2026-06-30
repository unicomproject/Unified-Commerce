using E_POS.Domain.Modules.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ReturnExchange.Configurations;

public sealed class SalesReturnEventConfiguration : IEntityTypeConfiguration<SalesReturnEvent>
{
    public void Configure(EntityTypeBuilder<SalesReturnEvent> builder)
    {
        builder.ToTable("sales_return_events");

        builder.HasKey(x => x.Id).HasName("pk_sales_return_events");

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

        builder.Property(x => x.SalesReturnId)
            .HasColumnName("sales_return_id")
            .IsRequired(false);

        builder.Property(x => x.SequenceNumber)
            .HasColumnName("sequence_number");

        builder.HasOne<SalesReturn>()
            .WithMany()
            .HasForeignKey(x => x.SalesReturnId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_return_events_sales_return_id_sales_returns");

        builder.HasIndex(x => new { x.SalesReturnId, x.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("uq_sales_return_events_sales_return_id_sequence_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_return_events_sequence_number", "sequence_number > 0")); 
    }
}

