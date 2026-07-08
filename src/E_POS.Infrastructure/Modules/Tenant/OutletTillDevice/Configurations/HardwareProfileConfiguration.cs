using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Configurations;

public sealed class HardwareProfileConfiguration : IEntityTypeConfiguration<HardwareProfile>
{
    public void Configure(EntityTypeBuilder<HardwareProfile> builder)
    {
        builder.ToTable("hardware_profiles");
        builder.HasKey(x => x.Id).HasName("pk_hardware_profiles");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ProfileName).HasColumnName("profile_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.ProfileType).HasColumnName("profile_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text").IsRequired(false);
        builder.Property(x => x.ConfigurationJson).HasColumnName("configuration_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedByTenantUserId).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedByTenantUserId).HasColumnName("updated_by_tenant_user_id").IsRequired(false);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_profiles_tenant_id_tenants");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_profiles_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedByTenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_hardware_profiles_updated_by_tenant_user_id_tenant_users");
        builder.HasIndex(x => new { x.TenantId, x.ProfileName }).IsUnique().HasDatabaseName("uq_hardware_profiles_tenant_id_profile_name");
        builder.ToTable(t => t.HasCheckConstraint("ck_hardware_profiles_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}
