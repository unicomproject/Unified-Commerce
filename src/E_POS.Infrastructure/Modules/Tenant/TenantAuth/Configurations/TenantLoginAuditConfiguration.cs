using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.TenantAuth.Configurations;

public sealed class TenantLoginAuditConfiguration : IEntityTypeConfiguration<TenantLoginAudit>
{
    public void Configure(EntityTypeBuilder<TenantLoginAudit> builder)
    {
        builder.ToTable("tenant_login_audits");

        builder.HasKey(x => x.Id).HasName("pk_tenant_login_audits");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.AuthSessionId).HasColumnName("auth_session_id");
        builder.Property(x => x.PosDeviceId).HasColumnName("pos_device_id");
        builder.Property(x => x.AttemptedIdentifier).HasColumnName("attempted_identifier").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(x => x.AuthenticationMethod).HasColumnName("authentication_method").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.LoginStatus).HasColumnName("login_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.FailureCode).HasColumnName("failure_code").HasColumnType("varchar(60)").HasMaxLength(60);
        builder.Property(x => x.FailureDetail).HasColumnName("failure_detail").HasColumnType("text");
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasColumnType("varchar(45)");
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasColumnType("text");
        builder.Property(x => x.AttemptedAt).HasColumnName("attempted_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_login_audits_tenant_id_tenants");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_login_audits_user_id_tenant_users");

        builder.HasOne<TenantAuthSession>()
            .WithMany()
            .HasForeignKey(x => x.AuthSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_login_audits_auth_session_id_tenant_auth_sessions");
    }
}



