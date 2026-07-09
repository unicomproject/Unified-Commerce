using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Configurations;

public sealed class PlatformLoginAuditConfiguration : IEntityTypeConfiguration<PlatformLoginAudit>
{
    public void Configure(EntityTypeBuilder<PlatformLoginAudit> builder)
    {
        builder.ToTable("platform_login_audits");

        builder.HasKey(x => x.Id).HasName("pk_platform_login_audits");

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

        builder.Property(x => x.PlatformUserId)
            .HasColumnName("platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.LoginResult)
            .HasColumnName("login_result")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.PlatformAuthSessionId)
            .HasColumnName("platform_auth_session_id");

        builder.Property(x => x.AuthenticationMethod)
            .HasColumnName("authentication_method")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.AttemptedAt)
            .HasColumnName("attempted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("varchar(45)");

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasColumnType("text");

        builder.Property(x => x.LoginStatus)
            .HasColumnName("login_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.FailureReason)
            .HasColumnName("failure_reason")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250);

        builder.Property(x => x.RiskScore)
            .HasColumnName("risk_score");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_login_audits_platform_user_id_platform_users");

        builder.HasOne<PlatformAuthSession>()
            .WithMany()
            .HasForeignKey(x => x.PlatformAuthSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_login_audits_platform_auth_session_id_platform_auth_sessions");

        builder.HasIndex(x => new { x.PlatformUserId, x.LoginResult, x.CreatedAt })
            .HasDatabaseName("ix_platform_login_audits_platform_user_id_login_result_created_at");

        builder.HasIndex(x => x.PlatformAuthSessionId)
            .HasDatabaseName("ix_platform_login_audits_platform_auth_session_id");

        builder.HasIndex(x => x.AttemptedAt)
            .HasDatabaseName("ix_platform_login_audits_attempted_at");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_platform_login_audits_login_result",
                "login_result IN ('SUCCESS', 'FAILED', 'LOCKED')");
            t.HasCheckConstraint(
                "ck_platform_login_audits_login_status",
                "login_status IN ('SUCCESS', 'FAILED', 'LOCKED')");
        });
    }
}
