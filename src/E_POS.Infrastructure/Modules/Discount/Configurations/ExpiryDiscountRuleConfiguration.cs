using E_POS.Domain.Modules.Discount.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Discount.Configurations;

public sealed class ExpiryDiscountRuleConfiguration : IEntityTypeConfiguration<ExpiryDiscountRule>
{
    public void Configure(EntityTypeBuilder<ExpiryDiscountRule> builder)
    {
        builder.ToTable("expiry_discount_rules");

        builder.HasKey(x => x.Id).HasName("pk_expiry_discount_rules");

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

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.RuleCode)
            .HasColumnName("rule_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_expiry_discount_rules_tenant_id_tenants");

        builder.HasIndex(x => new { x.TenantId, x.RuleCode })
            .IsUnique()
            .HasDatabaseName("uq_expiry_discount_rules_tenant_id_rule_code");
    }
}

