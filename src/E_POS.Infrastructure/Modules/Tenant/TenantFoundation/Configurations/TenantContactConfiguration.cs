using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantEntity = E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Configurations;

public sealed class TenantContactConfiguration : IEntityTypeConfiguration<TenantContact>
{
    public void Configure(EntityTypeBuilder<TenantContact> b)
    {
        b.ToTable("tenant_contacts", t =>
        {
            t.HasCheckConstraint("ck_tenant_contacts_type", "contact_type IN ('BILLING','SUPPORT')");
            t.HasCheckConstraint("ck_tenant_contacts_status", "status IN ('ACTIVE','INACTIVE')");
            t.HasCheckConstraint("ck_tenant_contacts_reachable", "contact_type <> 'SUPPORT' OR email IS NOT NULL OR phone IS NOT NULL");
            t.HasCheckConstraint("ck_tenant_contacts_billing_email", "contact_type <> 'BILLING' OR email IS NOT NULL");
        });
        b.HasKey(x => x.Id).HasName("pk_tenant_contacts");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(x => x.ContactType).HasColumnName("contact_type").HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        b.Property(x => x.ContactName).HasColumnName("contact_name").HasColumnType("varchar(200)").HasMaxLength(200).IsRequired();
        b.Property(x => x.Email).HasColumnName("email").HasColumnType("varchar(255)").HasMaxLength(255);
        b.Property(x => x.Phone).HasColumnName("phone").HasColumnType("varchar(40)").HasMaxLength(40);
        b.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        b.Property(x => x.CreatedByPlatformUserId).HasColumnName("created_by_platform_user_id").IsRequired();
        b.Property(x => x.UpdatedByPlatformUserId).HasColumnName("updated_by_platform_user_id").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        b.Ignore(x => x.CreatedBy); b.Ignore(x => x.UpdatedBy);
        b.HasOne<TenantEntity>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tenant_contacts_tenant");
        b.HasOne<E_POS.Domain.Modules.Platform.PlatformAdmin.Entities.PlatformUser>().WithMany().HasForeignKey(x => x.CreatedByPlatformUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tenant_contacts_created_by");
        b.HasOne<E_POS.Domain.Modules.Platform.PlatformAdmin.Entities.PlatformUser>().WithMany().HasForeignKey(x => x.UpdatedByPlatformUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tenant_contacts_updated_by");
        b.HasIndex(x => new { x.TenantId, x.ContactType }).IsUnique().HasFilter("status = 'ACTIVE'").HasDatabaseName("uq_tenant_contacts_active_type");
    }
}
