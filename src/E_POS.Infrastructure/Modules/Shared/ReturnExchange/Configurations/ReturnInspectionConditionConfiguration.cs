using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Configurations;

public sealed class ReturnInspectionConditionConfiguration
    : IEntityTypeConfiguration<ReturnInspectionCondition>
{
    public void Configure(EntityTypeBuilder<ReturnInspectionCondition> builder)
    {
        builder.ToTable("return_inspection_conditions", t =>
        {
            t.HasCheckConstraint(
                "ck_return_inspection_conditions_sort_order",
                "sort_order >= 0");
            t.HasCheckConstraint(
                "ck_return_inspection_conditions_status_category",
                "status_category IN ('GOOD', 'WARNING', 'DANGER')");
            t.HasCheckConstraint(
                "ck_return_inspection_conditions_refund_impact",
                "refund_impact IN ('NONE', 'PARTIAL', 'FULL_DENIAL')");
        });

        builder.HasKey(x => x.Id).HasName("pk_return_inspection_conditions");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ConditionCode)
            .HasColumnName("condition_code")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();
        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);
        builder.Property(x => x.StatusCategory)
            .HasColumnName("status_category")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(x => x.IsResellable).HasColumnName("is_resellable").IsRequired();
        builder.Property(x => x.RefundImpact)
            .HasColumnName("refund_impact")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(x => x.RequiresNotes).HasColumnName("requires_notes").IsRequired();
        builder.Property(x => x.RequiresPhoto).HasColumnName("requires_photo").IsRequired();
        builder.Property(x => x.RequiresApproval).HasColumnName("requires_approval").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ConditionCode })
            .IsUnique()
            .HasDatabaseName("ux_return_inspection_conditions_tenant_code");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_return_inspection_conditions_tenant_id_id");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_inspection_conditions_tenant");
    }
}
