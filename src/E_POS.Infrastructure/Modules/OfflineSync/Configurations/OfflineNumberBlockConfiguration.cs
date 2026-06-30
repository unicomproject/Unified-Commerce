using E_POS.Domain.Modules.OfflineSync.Entities;
using E_POS.Domain.Modules.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.OfflineSync.Configurations;

public sealed class OfflineNumberBlockConfiguration : IEntityTypeConfiguration<OfflineNumberBlock>
{
    public void Configure(EntityTypeBuilder<OfflineNumberBlock> builder)
    {
        builder.ToTable("offline_number_blocks");

        builder.HasKey(x => x.Id).HasName("pk_offline_number_blocks");

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

        builder.Property(x => x.OfflineClientId)
            .HasColumnName("offline_client_id")
            .IsRequired();

        builder.Property(x => x.DocumentNumberSequenceId)
            .HasColumnName("document_number_sequence_id")
            .IsRequired();

        builder.Property(x => x.NextValue)
            .HasColumnName("next_value");

        builder.Property(x => x.PaddingLengthSnapshot)
            .HasColumnName("padding_length_snapshot");

        builder.Property(x => x.RangeEnd)
            .HasColumnName("range_end");

        builder.Property(x => x.RangeStart)
            .HasColumnName("range_start");

        builder.HasOne<OfflineClient>()
            .WithMany()
            .HasForeignKey(x => x.OfflineClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_number_blocks_offline_client_id_offline_clients");

        builder.HasOne<DocumentNumberSequence>()
            .WithMany()
            .HasForeignKey(x => x.DocumentNumberSequenceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_number_blocks_document_number_sequence_id_document_number_sequences");

        builder.ToTable(t => t.HasCheckConstraint("ck_offline_number_blocks_range_start", "range_start > 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_offline_number_blocks_range_end_range_start", "range_end >= range_start")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_offline_number_blocks_next_value_range_start", "next_value >= range_start")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_offline_number_blocks_next_value_range_end", "next_value <= range_end + 1")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_offline_number_blocks_padding_length_snapshot", "padding_length_snapshot > 0")); 
    }
}

