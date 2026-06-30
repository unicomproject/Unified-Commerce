using E_POS.Domain.Modules.Discount.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Discount.Configurations;

public sealed class DiscountPolicyTargetConfiguration : IEntityTypeConfiguration<DiscountPolicyTarget>
{
    public void Configure(EntityTypeBuilder<DiscountPolicyTarget> builder)
    {
        builder.ToTable("discount_policy_targets");

        builder.HasKey(x => x.Id).HasName("pk_discount_policy_targets");

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

        builder.Property(x => x.DiscountPolicyId)
            .HasColumnName("discount_policy_id")
            .IsRequired();

        builder.Property(x => x.TargetId)
            .HasColumnName("target_id");

        builder.Property(x => x.TargetType)
            .HasColumnName("target_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.HasOne<DiscountPolicy>()
            .WithMany()
            .HasForeignKey(x => x.DiscountPolicyId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_discount_policy_targets_discount_policy_id_discount_policies");

        builder.HasIndex(x => new { x.DiscountPolicyId, x.TargetType, x.TargetId })
            .IsUnique()
            .HasDatabaseName("uq_discount_policy_targets_discount_policy_id_target_type_target_id");
    }
}

