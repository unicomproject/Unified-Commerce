using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Configurations;

public sealed class ReturnInspectionDraftConfiguration : IEntityTypeConfiguration<ReturnInspectionDraft>
{
    public void Configure(EntityTypeBuilder<ReturnInspectionDraft> builder)
    {
        builder.ToTable("return_inspection_drafts", t =>
        {
            t.HasCheckConstraint(
                "ck_return_inspection_drafts_status",
                "status IN ('DRAFT', 'VALIDATED', 'CONSUMED', 'CANCELLED')");
            t.HasCheckConstraint(
                "ck_return_inspection_drafts_version",
                "version >= 1");
            t.HasCheckConstraint(
                "ck_return_inspection_drafts_expires_at",
                "expires_at > created_at");
            t.HasCheckConstraint(
                "ck_return_inspection_drafts_resolution_type",
                "resolution_type IS NULL OR resolution_type IN ('REFUND', 'EXCHANGE')");
            t.HasCheckConstraint(
                "ck_return_inspection_drafts_resolution_fields",
                "(resolution_type IS NULL AND resolution_selected_at IS NULL AND resolution_selected_by_tenant_user_id IS NULL) OR " +
                "(resolution_type IS NOT NULL AND resolution_selected_at IS NOT NULL AND resolution_selected_by_tenant_user_id IS NOT NULL)");
        });

        builder.HasKey(x => x.Id).HasName("pk_return_inspection_drafts");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.SaleId).HasColumnName("sale_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").IsRequired().HasDefaultValue(1).IsConcurrencyToken();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ValidatedAt).HasColumnName("validated_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ValidatedByTenantUserId).HasColumnName("validated_by_tenant_user_id");
        builder.Property(x => x.RequiresInspection).HasColumnName("requires_inspection").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.RequiresManagerApproval).HasColumnName("requires_manager_approval").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired();
        builder.Property(x => x.ResolutionType).HasColumnName("resolution_type").HasColumnType("varchar(20)").HasMaxLength(20);
        builder.Property(x => x.ResolutionSelectedAt).HasColumnName("resolution_selected_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ResolutionSelectedByTenantUserId).HasColumnName("resolution_selected_by_tenant_user_id");
        builder.Property(x => x.RefundMethodCode).HasColumnName("refund_method_code").HasColumnType("varchar(40)").HasMaxLength(40);
        builder.Property(x => x.RefundMethodSelectedAt).HasColumnName("refund_method_selected_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.RefundMethodSelectedByTenantUserId).HasColumnName("refund_method_selected_by_tenant_user_id");

        builder.HasIndex(x => new { x.TenantId, x.OutletId, x.SaleId })
            .IsUnique()
            .HasDatabaseName("ux_return_inspection_drafts_sale");
        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_return_inspection_drafts_tenant_id_id");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities.Outlet>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.OutletId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_inspection_drafts_outlet_tenant");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SaleId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_inspection_drafts_sale_tenant");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ResolutionSelectedByTenantUserId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_inspection_drafts_resolution_user_tenant");
    }
}
