using E_POS.Domain.Modules.Discount.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Discount.Configurations;

public sealed class DiscountPolicyOutletConfiguration : IEntityTypeConfiguration<DiscountPolicyOutlet>
{
    public void Configure(EntityTypeBuilder<DiscountPolicyOutlet> builder)
    {
        builder.ToTable("discount_policy_outlets");

        builder.HasKey(x => x.Id).HasName("pk_discount_policy_outlets");

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

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.DiscountPolicyId)
            .HasColumnName("discount_policy_id")
            .IsRequired();

        builder.HasOne<DiscountPolicy>()
            .WithMany()
            .HasForeignKey(x => x.DiscountPolicyId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_discount_policy_outlets_discount_policy_id_discount_policies");

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => x.OutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_discount_policy_outlets_outlet_id_outlets");

        builder.HasIndex(x => new { x.DiscountPolicyId, x.OutletId })
            .IsUnique()
            .HasDatabaseName("uq_discount_policy_outlets_discount_policy_id_outlet_id");
    }
}

