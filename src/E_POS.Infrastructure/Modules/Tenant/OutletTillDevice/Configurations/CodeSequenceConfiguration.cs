using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Configurations;

public sealed class CodeSequenceConfiguration : IEntityTypeConfiguration<CodeSequence>
{
    public void Configure(EntityTypeBuilder<CodeSequence> builder)
    {
        builder.ToTable("code_sequences");

        builder.HasKey(x => x.Id).HasName("pk_code_sequences");

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

        builder.Property(x => x.SequenceKey)
            .HasColumnName("sequence_key")
            .HasColumnType("varchar(160)")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.Prefix)
            .HasColumnName("prefix")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CurrentValue)
            .HasColumnName("current_value")
            .IsRequired();

        builder.Property(x => x.PaddingLength)
            .HasColumnName("padding_length")
            .IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_code_sequences_tenant_id_tenants");

        builder.HasIndex(x => new { x.TenantId, x.SequenceKey })
            .IsUnique()
            .HasDatabaseName("uq_code_sequences_tenant_id_sequence_key");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_code_sequences_current_value", "current_value >= 0");
            t.HasCheckConstraint("ck_code_sequences_padding_length", "padding_length > 0");
        });
    }
}


