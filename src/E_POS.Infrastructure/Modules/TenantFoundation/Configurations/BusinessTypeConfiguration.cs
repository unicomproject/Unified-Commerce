using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.TenantFoundation.Configurations;

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

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.BusinessTypeCode)
            .HasColumnName("business_type_code")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.HasIndex(x => x.BusinessTypeCode)
            .IsUnique()
            .HasDatabaseName("uq_business_types_business_type_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_business_types_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')")); 
    }
}

