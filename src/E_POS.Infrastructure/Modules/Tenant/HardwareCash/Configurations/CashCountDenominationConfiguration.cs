using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities; // for Currency and Tenant
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Configurations;

public sealed class CashCountDenominationConfiguration : IEntityTypeConfiguration<CashCountDenomination>
{
    public void Configure(EntityTypeBuilder<CashCountDenomination> builder)
    {
        builder.ToTable("cash_count_denominations");

        builder.HasKey(x => x.Id).HasName("pk_cash_count_denominations");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.CashReconciliationId).HasColumnName("cash_reconciliation_id").IsRequired();
        builder.Property(x => x.CountType).HasColumnName("count_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasColumnType("char(3)").HasMaxLength(3).IsRequired();
        builder.Property(x => x.DenominationValue).HasColumnName("denomination_value").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasColumnType("integer").IsRequired();
        builder.Property(x => x.LineTotal).HasColumnName("line_total").HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CountedByTenantUserId).HasColumnName("counted_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.CountedAt).HasColumnName("counted_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_count_denominations_tenant_id_tenants");
        builder.HasOne<CashReconciliation>().WithMany().HasForeignKey(x => x.CashReconciliationId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_count_denominations_cash_reconciliation_id_cash_reconciliations");
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).HasPrincipalKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_count_denominations_currency_code_currencies");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CountedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_count_denominations_counted_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.CashReconciliationId, x.CountType, x.CurrencyCode, x.DenominationValue })
            .IsUnique()
            .HasDatabaseName("uq_cash_count_denominations_reconciliation_type_currency_value");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_cash_count_denominations_denomination_value", "denomination_value > 0");
            t.HasCheckConstraint("ck_cash_count_denominations_quantity", "quantity >= 0");
            t.HasCheckConstraint("ck_cash_count_denominations_count_type", "count_type <> ''");
        });
    }
}



