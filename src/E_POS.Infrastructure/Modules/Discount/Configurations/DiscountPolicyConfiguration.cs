using E_POS.Domain.Modules.Discount.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Discount.Configurations;

public sealed class DiscountPolicyConfiguration : IEntityTypeConfiguration<DiscountPolicy>
{
    public void Configure(EntityTypeBuilder<DiscountPolicy> builder)
    {
        builder.ToTable("discount_policies");

        builder.HasKey(x => x.Id).HasName("pk_discount_policies");

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

        builder.Property(x => x.DiscountCode)
            .HasColumnName("discount_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.DiscountTypeId)
            .HasColumnName("discount_type_id")
            .IsRequired();

        builder.Property(x => x.DiscountValue)
            .HasColumnName("discount_value")
            .HasPrecision(18, 2);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_discount_policies_tenant_id_tenants");

        builder.HasOne<DiscountType>()
            .WithMany()
            .HasForeignKey(x => x.DiscountTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_discount_policies_discount_type_id_discount_types");

        builder.HasIndex(x => new { x.TenantId, x.DiscountCode })
            .IsUnique()
            .HasDatabaseName("uq_discount_policies_tenant_id_discount_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_discount_policies_discount_value", "discount_value >= 0")); 
    }
}

