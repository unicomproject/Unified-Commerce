using E_POS.Domain.Modules.Discount.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Discount.Configurations;

public sealed class DiscountPolicyConditionConfiguration : IEntityTypeConfiguration<DiscountPolicyCondition>
{
    public void Configure(EntityTypeBuilder<DiscountPolicyCondition> builder)
    {
        builder.ToTable("discount_policy_conditions");

        builder.HasKey(x => x.Id).HasName("pk_discount_policy_conditions");

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

        builder.Property(x => x.ConditionSequence)
            .HasColumnName("condition_sequence");

        builder.Property(x => x.DiscountPolicyId)
            .HasColumnName("discount_policy_id")
            .IsRequired();

        builder.HasOne<DiscountPolicy>()
            .WithMany()
            .HasForeignKey(x => x.DiscountPolicyId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_discount_policy_conditions_discount_policy_id_discount_policies");

        builder.ToTable(t => t.HasCheckConstraint("ck_discount_policy_conditions_condition_sequence", "condition_sequence > 0")); 
    }
}

