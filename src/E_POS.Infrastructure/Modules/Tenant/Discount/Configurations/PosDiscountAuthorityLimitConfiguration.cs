using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Discount.Configurations;

public sealed class PosDiscountAuthorityLimitConfiguration : IEntityTypeConfiguration<PosDiscountAuthorityLimit>
{
    public void Configure(EntityTypeBuilder<PosDiscountAuthorityLimit> builder)
    {
        builder.ToTable("pos_discount_authority_limits", t =>
        {
            t.HasCheckConstraint("ck_pos_discount_authority_limits_percentage", "max_percentage >= 0 AND max_percentage <= 100");
            t.HasCheckConstraint("ck_pos_discount_authority_limits_fixed", "max_fixed_amount >= 0");
            t.HasCheckConstraint("ck_pos_discount_authority_limits_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        });
        builder.HasKey(x => x.Id).HasName("pk_pos_discount_authority_limits");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.TenantUserId).HasColumnName("tenant_user_id").IsRequired();
        builder.Property(x => x.MaxPercentage).HasColumnName("max_percentage").HasPrecision(8, 4).IsRequired();
        builder.Property(x => x.MaxFixedAmount).HasColumnName("max_fixed_amount").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasColumnType("char(3)").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id");
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id");
        builder.HasIndex(x => new { x.TenantId, x.TenantUserId }).IsUnique().HasDatabaseName("uq_pos_discount_authority_limits_tenant_user");
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.TenantUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).HasPrincipalKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
    }
}
