using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class ReturnPolicyConfiguration : IEntityTypeConfiguration<ReturnPolicy>
{
    public void Configure(EntityTypeBuilder<ReturnPolicy> builder)
    {
        builder.ToTable("return_policies");

        builder.HasKey(x => x.Id).HasName("pk_return_policies");

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

        builder.Property(x => x.PolicyCode)
            .HasColumnName("policy_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.ReturnWindowDays)
            .HasColumnName("return_window_days");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_return_policies_tenant_id_tenants");

        builder.HasIndex(x => new { x.TenantId, x.PolicyCode })
            .IsUnique()
            .HasDatabaseName("uq_return_policies_tenant_id_policy_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_return_policies_return_window_days", "return_window_days IS NULL OR return_window_days >= 0")); 
    }
}

