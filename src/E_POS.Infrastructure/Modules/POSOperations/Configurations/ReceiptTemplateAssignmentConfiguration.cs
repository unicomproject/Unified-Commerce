using E_POS.Domain.Modules.POSOperations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.POSOperations.Configurations;

public sealed class ReceiptTemplateAssignmentConfiguration : IEntityTypeConfiguration<ReceiptTemplateAssignment>
{
    public void Configure(EntityTypeBuilder<ReceiptTemplateAssignment> builder)
    {
        builder.ToTable("receipt_template_assignments");

        builder.HasKey(x => x.Id).HasName("pk_receipt_template_assignments");

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

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id");

        builder.Property(x => x.TillId)
            .HasColumnName("till_id");

        builder.Property(x => x.PosDeviceId)
            .HasColumnName("pos_device_id");

        builder.Property(x => x.AssignmentScope)
            .HasColumnName("assignment_scope")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.ReceiptTemplateVersionId)
            .HasColumnName("receipt_template_version_id")
            .IsRequired();

        builder.HasOne<ReceiptTemplateVersion>()
            .WithMany()
            .HasForeignKey(x => x.ReceiptTemplateVersionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_template_assignments_receipt_template_version_id_receipt_template_versions");

        builder.ToTable(t => t.HasCheckConstraint("ck_receipt_template_assignments_assignment_scope_outlet_id_till_id", "(assignment_scope = 'OUTLET' AND outlet_id IS NOT NULL) OR (assignment_scope = 'TILL' AND till_id IS NOT NULL) OR (assignment_scope = 'POS_DEVICE' AND pos_device_id IS NOT NULL)")); 
    }
}

