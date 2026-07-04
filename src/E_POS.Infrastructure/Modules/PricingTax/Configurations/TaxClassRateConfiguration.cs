using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.PricingTax.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.PricingTax.Configurations;

public sealed class TaxClassRateConfiguration : IEntityTypeConfiguration<TaxClassRate>
{
    public void Configure(EntityTypeBuilder<TaxClassRate> builder)
    {
        builder.ToTable("tax_class_rates");

        builder.HasKey(x => x.Id).HasName("pk_tax_class_rates");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.TaxClassId).HasColumnName("tax_class_id").IsRequired();
        builder.Property(x => x.TaxRateId).HasColumnName("tax_rate_id").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tax_class_rates_tenant_id_tenants");
        builder.HasOne<TaxClass>().WithMany().HasForeignKey(x => x.TaxClassId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tax_class_rates_tax_class_id_tax_classes");
        builder.HasOne<TaxRate>().WithMany().HasForeignKey(x => x.TaxRateId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tax_class_rates_tax_rate_id_tax_rates");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tax_class_rates_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tax_class_rates_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.TaxClassId, x.TaxRateId }).IsUnique().HasDatabaseName("uq_tax_class_rates_tenant_id_tax_class_id_tax_rate_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_tax_class_rates_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
            t.HasCheckConstraint("ck_tax_class_rates_sort_order", "sort_order >= 0");
        });
    }
}
