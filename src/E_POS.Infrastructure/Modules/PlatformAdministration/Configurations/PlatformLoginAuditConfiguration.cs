using E_POS.Domain.Modules.PlatformAdministration.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.PlatformAdministration.Configurations;

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

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_login_audits_platform_user_id_platform_users");

        builder.HasIndex(x => new { x.PlatformUserId, x.LoginResult, x.CreatedAt })
            .HasDatabaseName("ix_platform_login_audits_platform_user_id_login_result_created_at");

        builder.ToTable(t => t.HasCheckConstraint("ck_platform_login_audits_login_result", "login_result IN ('SUCCESS', 'FAILED', 'LOCKED')")); 
    }
}

