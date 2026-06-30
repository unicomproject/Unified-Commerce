using E_POS.Domain.Modules.Discount.Entities;
using E_POS.Domain.Modules.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Discount.Configurations;

public sealed class ExpiryDiscountApplicationConfiguration : IEntityTypeConfiguration<ExpiryDiscountApplication>
{
    public void Configure(EntityTypeBuilder<ExpiryDiscountApplication> builder)
    {
        builder.ToTable("expiry_discount_applications");

        builder.HasKey(x => x.Id).HasName("pk_expiry_discount_applications");

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

        builder.Property(x => x.ExpiryDiscountRuleId)
            .HasColumnName("expiry_discount_rule_id")
            .IsRequired();

        builder.Property(x => x.ProductBatchId)
            .HasColumnName("product_batch_id")
            .IsRequired(false);

        builder.HasOne<ExpiryDiscountRule>()
            .WithMany()
            .HasForeignKey(x => x.ExpiryDiscountRuleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_expiry_discount_applications_expiry_discount_rule_id_expiry_discount_rules");

        builder.HasOne<ProductBatch>()
            .WithMany()
            .HasForeignKey(x => x.ProductBatchId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_expiry_discount_applications_product_batch_id_product_batches");

        builder.HasIndex(x => new { x.ProductBatchId, x.ExpiryDiscountRuleId })
            .IsUnique()
            .HasDatabaseName("uq_expiry_discount_applications_product_batch_id_expiry_discount_rule_id");
    }
}

