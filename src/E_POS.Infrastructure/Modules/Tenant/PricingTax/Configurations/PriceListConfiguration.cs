using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.PricingTax.Configurations;

public sealed class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.ToTable("price_lists");

        builder.HasKey(x => x.Id).HasName("pk_price_lists");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.PriceListCode).HasColumnName("price_list_code").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.PriceListName).HasColumnName("price_list_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.PriceListType).HasColumnName("price_list_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasColumnType("char(3)").HasMaxLength(3).IsRequired();
        builder.Property(x => x.PriceIncludesTax).HasColumnName("price_includes_tax").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.IsDefaultPriceList).HasColumnName("is_default_price_list").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Priority).HasColumnName("priority").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnName("valid_from").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.ValidUntil).HasColumnName("valid_until").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_lists_tenant_id_tenants");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).HasPrincipalKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_lists_currency_code_currencies");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_lists_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_lists_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.PriceListCode }).IsUnique().HasDatabaseName("uq_price_lists_tenant_id_price_list_code");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_price_lists_tenant_id_id");
        builder.HasIndex(x => x.TenantId).IsUnique().HasDatabaseName("uq_price_lists_active_default_per_tenant").HasFilter("is_default_price_list = true AND status = 'ACTIVE'");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_price_lists_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
            t.HasCheckConstraint("ck_price_lists_priority", "priority >= 0");
            t.HasCheckConstraint("ck_price_lists_valid_period", "valid_until IS NULL OR valid_from IS NULL OR valid_until >= valid_from");
        });
    }
}



