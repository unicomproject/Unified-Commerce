using E_POS.Domain.Modules.Tenant.OfflineSync.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OfflineSync.Configurations;

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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.OfflineClientId)
            .HasColumnName("offline_client_id")
            .IsRequired();

        builder.Property(x => x.DocumentNumberSequenceId)
            .HasColumnName("document_number_sequence_id")
            .IsRequired(false);

        builder.Property(x => x.DocumentType)
            .HasColumnName("document_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.PrefixSnapshot)
            .HasColumnName("prefix_snapshot")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(x => x.SuffixSnapshot)
            .HasColumnName("suffix_snapshot")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(x => x.PaddingLengthSnapshot)
            .HasColumnName("padding_length_snapshot")
            .IsRequired();

        builder.Property(x => x.RangeStart)
            .HasColumnName("range_start")
            .IsRequired();

        builder.Property(x => x.RangeEnd)
            .HasColumnName("range_end")
            .IsRequired();

        builder.Property(x => x.NextValue)
            .HasColumnName("next_value")
            .IsRequired();

        builder.Property(x => x.BlockStatus)
            .HasColumnName("block_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.AllocatedAt)
            .HasColumnName("allocated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.ExhaustedAt)
            .HasColumnName("exhausted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_number_blocks_a2188000");

        builder.HasOne<E_POS.Domain.Modules.Tenant.OfflineSync.Entities.OfflineClient>()
            .WithMany()
            .HasForeignKey(x => x.OfflineClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_number_blocks_1346232d");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.DocumentNumberSequence>()
            .WithMany()
            .HasForeignKey(x => x.DocumentNumberSequenceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_number_blocks_fae23a2a");
        // <second-brain-checks>
        builder.ToTable(t => t.HasCheckConstraint("ck_offline_number_blocks_cb2aa85a", "range_start > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_offline_number_blocks_52545072", "range_end >= range_start"));
        builder.ToTable(t => t.HasCheckConstraint("ck_offline_number_blocks_19fcb4b9", "next_value >= range_start"));
        builder.ToTable(t => t.HasCheckConstraint("ck_offline_number_blocks_cc44e5d4", "next_value <= range_end + 1"));
        builder.ToTable(t => t.HasCheckConstraint("ck_offline_number_blocks_c7dcd2aa", "padding_length_snapshot > 0"));
        // </second-brain-checks>

        // </second-brain-constraints>
    }
}

