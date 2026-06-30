using E_POS.Domain.Modules.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Orders.Configurations;

public sealed class DocumentNumberSequenceConfiguration : IEntityTypeConfiguration<DocumentNumberSequence>
{
    public void Configure(EntityTypeBuilder<DocumentNumberSequence> builder)
    {
        builder.ToTable("document_number_sequences");

        builder.HasKey(x => x.Id).HasName("pk_document_number_sequences");

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

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id");

        builder.Property(x => x.DocumentType)
            .HasColumnName("document_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.DocumentSubtype)
            .HasColumnName("document_subtype")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.CurrentValue)
            .HasColumnName("current_value");

        builder.Property(x => x.PaddingLength)
            .HasColumnName("padding_length");

        builder.Property(x => x.SalesChannelId)
            .HasColumnName("sales_channel_id");

        builder.HasIndex(x => new { x.TenantId, x.DocumentType, x.DocumentSubtype, x.SalesChannelId, x.OutletId })
            .IsUnique()
            .HasDatabaseName("uq_document_number_sequences_tenant_id_document_type_document_subtype_sales_channel_id_outlet_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_document_number_sequences_padding_length", "padding_length > 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_document_number_sequences_current_value", "current_value >= 0")); 
    }
}

