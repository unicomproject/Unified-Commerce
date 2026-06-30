using E_POS.Domain.Modules.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ReturnExchange.Configurations;

public sealed class SalesExchangeEventConfiguration : IEntityTypeConfiguration<SalesExchangeEvent>
{
    public void Configure(EntityTypeBuilder<SalesExchangeEvent> builder)
    {
        builder.ToTable("sales_exchange_events");

        builder.HasKey(x => x.Id).HasName("pk_sales_exchange_events");

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

        builder.Property(x => x.SalesExchangeId)
            .HasColumnName("sales_exchange_id")
            .IsRequired();

        builder.Property(x => x.SequenceNumber)
            .HasColumnName("sequence_number");

        builder.HasOne<SalesExchange>()
            .WithMany()
            .HasForeignKey(x => x.SalesExchangeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_exchange_events_sales_exchange_id_sales_exchanges");

        builder.HasIndex(x => new { x.SalesExchangeId, x.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("uq_sales_exchange_events_sales_exchange_id_sequence_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_sales_exchange_events_sequence_number", "sequence_number > 0")); 
    }
}

