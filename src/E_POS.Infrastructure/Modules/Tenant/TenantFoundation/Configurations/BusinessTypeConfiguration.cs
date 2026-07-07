using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Configurations;

public sealed class BusinessTypeConfiguration : IEntityTypeConfiguration<BusinessType>
{
    public void Configure(EntityTypeBuilder<BusinessType> builder)
    {
        builder.ToTable("business_types");

        builder.HasKey(x => x.Id).HasName("pk_business_types");

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

        builder.Property(x => x.BusinessCode)
            .HasColumnName("business_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.BusinessName)
            .HasColumnName("business_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(x => x.BusinessCode)
            .IsUnique()
            .HasDatabaseName("ix_business_types_business_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_business_types_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')")); 
    }
}
