using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Configurations;

public sealed class ReturnReasonConfiguration : IEntityTypeConfiguration<ReturnReason>
{
    public void Configure(EntityTypeBuilder<ReturnReason> builder)
    {
        builder.ToTable("return_reasons", t =>
        {
            t.HasCheckConstraint(
                "ck_return_reasons_applies_to",
                "applies_to IN ('RETURN', 'EXCHANGE', 'BOTH')");
            t.HasCheckConstraint(
                "ck_return_reasons_sort_order",
                "sort_order >= 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_return_reasons");

        builder.Property(x => x.Id)
            .HasColumnName("id");

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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.ReasonCode)
            .HasColumnName("reason_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.ReasonName)
            .HasColumnName("reason_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.AppliesTo)
            .HasColumnName("applies_to")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.RequiresNote)
            .HasColumnName("requires_note")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.RequiresInspection)
            .HasColumnName("requires_inspection")
            .IsRequired();

        builder.Property(x => x.RequiresManagerApproval)
            .HasColumnName("requires_manager_approval")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.ReasonCode })
            .IsUnique()
            .HasDatabaseName("ux_return_reasons_978380c7");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_return_reasons_tenant_id_id");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_reasons_83eeafe4");
        // </second-brain-constraints>
    }
}
