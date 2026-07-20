using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Configurations;

public sealed class ReturnExchangeReplacementDraftLineConfiguration
    : IEntityTypeConfiguration<ReturnExchangeReplacementDraftLine>
{
    public void Configure(EntityTypeBuilder<ReturnExchangeReplacementDraftLine> builder)
    {
        builder.ToTable("return_exchange_replacement_draft_lines", table =>
        {
            table.HasCheckConstraint(
                "ck_return_exchange_replacement_draft_lines_quantity",
                "quantity > 0");
        });
        builder.HasKey(x => x.Id).HasName("pk_return_exchange_replacement_draft_lines");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ReturnInspectionDraftId).HasColumnName("return_inspection_draft_id").IsRequired();
        builder.Property(x => x.ReturnedSaleLineId).HasColumnName("returned_sale_line_id").IsRequired();
        builder.Property(x => x.ReplacementProductId).HasColumnName("replacement_product_id").IsRequired();
        builder.Property(x => x.ReplacementVariantId).HasColumnName("replacement_variant_id").IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.SelectedByTenantUserId).HasColumnName("selected_by_tenant_user_id").IsRequired();
        builder.Property(x => x.SelectedAt).HasColumnName("selected_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.ReturnInspectionDraftId, x.ReturnedSaleLineId })
            .IsUnique()
            .HasDatabaseName("ux_return_exchange_replacement_draft_line");
        builder.HasOne<ReturnInspectionDraft>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReturnInspectionDraftId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_exchange_replacement_draft_lines_draft_tenant");
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SelectedByTenantUserId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_exchange_replacement_draft_lines_user_tenant");
        builder.HasOne<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrderLine>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReturnedSaleLineId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_exchange_replacement_draft_lines_sale_line_tenant");
        builder.HasOne<E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.Product>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReplacementProductId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_exchange_replacement_draft_lines_product_tenant");
        builder.HasOne<E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.ProductVariant>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReplacementVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_exchange_replacement_draft_lines_variant_tenant");
    }
}
