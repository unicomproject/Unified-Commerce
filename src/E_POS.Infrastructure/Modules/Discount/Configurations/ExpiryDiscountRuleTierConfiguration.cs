using E_POS.Domain.Modules.Discount.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Discount.Configurations;

public sealed class ExpiryDiscountRuleTierConfiguration : IEntityTypeConfiguration<ExpiryDiscountRuleTier>
{
    public void Configure(EntityTypeBuilder<ExpiryDiscountRuleTier> builder)
    {
        builder.ToTable("expiry_discount_rule_tiers");

        builder.HasKey(x => x.Id).HasName("pk_expiry_discount_rule_tiers");

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

        builder.Property(x => x.DaysBeforeExpiry)
            .HasColumnName("days_before_expiry");

        builder.Property(x => x.DiscountValue)
            .HasColumnName("discount_value")
            .HasPrecision(18, 2);

        builder.Property(x => x.ExpiryDiscountRuleId)
            .HasColumnName("expiry_discount_rule_id")
            .IsRequired();

        builder.HasOne<ExpiryDiscountRule>()
            .WithMany()
            .HasForeignKey(x => x.ExpiryDiscountRuleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_expiry_discount_rule_tiers_expiry_discount_rule_id_expiry_discount_rules");

        builder.ToTable(t => t.HasCheckConstraint("ck_expiry_discount_rule_tiers_days_before_expiry", "days_before_expiry >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_expiry_discount_rule_tiers_discount_value", "discount_value >= 0")); 
    }
}

