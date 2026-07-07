using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Configurations;

public sealed class HardwareProfileConfiguration : IEntityTypeConfiguration<HardwareProfile>
{
    public void Configure(EntityTypeBuilder<HardwareProfile> builder)
    {
        builder.ToTable("hardware_profiles");

        builder.HasKey(x => x.Id).HasName("pk_hardware_profiles");

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

        builder.Property(x => x.ProfileCode)
            .HasColumnName("profile_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_hardware_profiles_tenant_id_tenants");

        builder.HasIndex(x => new { x.TenantId, x.ProfileCode })
            .IsUnique()
            .HasDatabaseName("uq_hardware_profiles_tenant_id_profile_code");
    }
}




