using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.AuthSecurity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.AuthSecurity.Configurations;

public sealed class TenantLoginAuditConfiguration : IEntityTypeConfiguration<TenantLoginAudit>
{
    public void Configure(EntityTypeBuilder<TenantLoginAudit> builder)
    {
        builder.ToTable("tenant_login_audits");

        builder.HasKey(x => x.Id).HasName("pk_tenant_login_audits");

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

        builder.Property(x => x.TenantUserId)
            .HasColumnName("tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.LoginResult)
            .HasColumnName("login_result")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.TenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_login_audits_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantUserId, x.LoginResult, x.CreatedAt })
            .HasDatabaseName("ix_tenant_login_audits_tenant_user_id_login_result_created_at");

        builder.ToTable(t => t.HasCheckConstraint("ck_tenant_login_audits_login_result", "login_result IN ('SUCCESS', 'FAILED', 'LOCKED')")); 
    }
}

