using E_POS.Domain.Modules.Discount.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Discount.Configurations;

public sealed class DiscountPolicyChannelConfiguration : IEntityTypeConfiguration<DiscountPolicyChannel>
{
    public void Configure(EntityTypeBuilder<DiscountPolicyChannel> builder)
    {
        builder.ToTable("discount_policy_channels");

        builder.HasKey(x => x.Id).HasName("pk_discount_policy_channels");

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

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.DiscountPolicyId)
            .HasColumnName("discount_policy_id")
            .IsRequired();

        builder.Property(x => x.SalesChannelId)
            .HasColumnName("sales_channel_id")
            .IsRequired();

        builder.HasOne<DiscountPolicy>()
            .WithMany()
            .HasForeignKey(x => x.DiscountPolicyId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_discount_policy_channels_discount_policy_id_discount_policies");
        builder.HasOne<SalesChannel>()
            .WithMany()
            .HasForeignKey(x => x.SalesChannelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_discount_policy_channels_sales_channel_id_sales_channels");

        builder.HasIndex(x => new { x.DiscountPolicyId, x.SalesChannelId })
            .IsUnique()
            .HasDatabaseName("uq_discount_policy_channels_discount_policy_id_sales_channel_id");
    }
}


