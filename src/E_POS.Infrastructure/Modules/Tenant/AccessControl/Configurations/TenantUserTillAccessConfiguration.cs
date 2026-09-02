using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Configurations;

public sealed class TenantUserTillAccessConfiguration : IEntityTypeConfiguration<TenantUserTillAccess>
{
    public void Configure(EntityTypeBuilder<TenantUserTillAccess> builder)
    {
        builder.ToTable("tenant_user_till_access");
        builder.HasKey(x => x.Id).HasName("pk_tenant_user_till_access");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.TenantUserId).HasColumnName("tenant_user_id").IsRequired();
        builder.Property(x => x.TillId).HasColumnName("till_id").IsRequired();
        builder.Property(x => x.AssignedByTenantUserId).HasColumnName("assigned_by_tenant_user_id");
        builder.Property(x => x.RevokedByTenantUserId).HasColumnName("revoked_by_tenant_user_id");
        builder.Property(x => x.AssignedAt).HasColumnName("assigned_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_user_till_access_tenant_id_tenants");
        builder.HasOne<TenantUser>()
            .WithMany().HasForeignKey(x => x.TenantUserId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_user_till_access_tenant_user_id_tenant_users");
        builder.HasOne<Till>()
            .WithMany().HasForeignKey(x => x.TillId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_user_till_access_till_id_tills");
        builder.HasOne<TenantUser>()
            .WithMany().HasForeignKey(x => x.AssignedByTenantUserId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_user_till_access_assigned_by_tenant_user_id");
        builder.HasOne<TenantUser>()
            .WithMany().HasForeignKey(x => x.RevokedByTenantUserId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_user_till_access_revoked_by_tenant_user_id");

        builder.HasIndex(x => new { x.TenantId, x.TenantUserId, x.TillId })
            .IsUnique()
            .HasDatabaseName("uq_tenant_user_till_access_tenant_user_till");
        builder.HasIndex(x => new { x.TenantId, x.TillId })
            .HasDatabaseName("ix_tenant_user_till_access_tenant_till");
    }
}
