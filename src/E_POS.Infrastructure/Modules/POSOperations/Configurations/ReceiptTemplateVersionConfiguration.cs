using E_POS.Domain.Modules.POSOperations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.POSOperations.Configurations;

public sealed class ReceiptTemplateVersionConfiguration : IEntityTypeConfiguration<ReceiptTemplateVersion>
{
    public void Configure(EntityTypeBuilder<ReceiptTemplateVersion> builder)
    {
        builder.ToTable("receipt_template_versions");

        builder.HasKey(x => x.Id).HasName("pk_receipt_template_versions");

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

        builder.Property(x => x.ReceiptTemplateId)
            .HasColumnName("receipt_template_id")
            .IsRequired();

        builder.Property(x => x.VersionNumber)
            .HasColumnName("version_number");

        builder.HasOne<ReceiptTemplate>()
            .WithMany()
            .HasForeignKey(x => x.ReceiptTemplateId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_template_versions_receipt_template_id_receipt_templates");

        builder.HasIndex(x => new { x.ReceiptTemplateId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("uq_receipt_template_versions_receipt_template_id_version_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_receipt_template_versions_version_number", "version_number > 0")); 
    }
}

