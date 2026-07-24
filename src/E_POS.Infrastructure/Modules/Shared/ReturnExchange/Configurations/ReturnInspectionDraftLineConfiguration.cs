using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Configurations;

public sealed class ReturnInspectionDraftLineConfiguration : IEntityTypeConfiguration<ReturnInspectionDraftLine>
{
    public void Configure(EntityTypeBuilder<ReturnInspectionDraftLine> builder)
    {
        builder.ToTable("return_inspection_draft_lines", t =>
        {
            t.HasCheckConstraint(
                "ck_return_inspection_draft_lines_status",
                "inspection_status IN ('PENDING', 'INSPECTED')");
            t.HasCheckConstraint(
                "ck_return_inspection_draft_lines_notes_length",
                "inspection_notes IS NULL OR char_length(inspection_notes) <= 200");
        });

        builder.HasKey(x => x.Id).HasName("pk_return_inspection_draft_lines");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ReturnInspectionDraftId).HasColumnName("return_inspection_draft_id").IsRequired();
        builder.Property(x => x.SaleLineId).HasColumnName("sale_line_id").IsRequired();
        builder.Property(x => x.ConditionId).HasColumnName("condition_id");
        builder.Property(x => x.ConditionCodeSnapshot)
            .HasColumnName("condition_code_snapshot")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(x => x.InspectionNotes).HasColumnName("inspection_notes").HasColumnType("text");
        builder.Property(x => x.InspectionStatus)
            .HasColumnName("inspection_status")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.InspectedByTenantUserId).HasColumnName("inspected_by_tenant_user_id");
        builder.Property(x => x.InspectedAt).HasColumnName("inspected_at").HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.TenantId, x.ReturnInspectionDraftId, x.SaleLineId })
            .IsUnique()
            .HasDatabaseName("ux_return_inspection_draft_lines_sale_line");
        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_return_inspection_draft_lines_tenant_id_id");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ReturnInspectionDraft>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReturnInspectionDraftId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_inspection_draft_lines_draft_tenant");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SaleLineId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_inspection_draft_lines_sale_line_tenant");

        builder.HasOne<ReturnInspectionCondition>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ConditionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_inspection_draft_lines_condition_tenant");
    }
}
