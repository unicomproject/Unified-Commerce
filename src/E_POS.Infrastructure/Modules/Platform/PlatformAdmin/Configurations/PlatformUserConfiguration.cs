using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Configurations;

public sealed class PlatformUserConfiguration : IEntityTypeConfiguration<PlatformUser>
{
    public void Configure(EntityTypeBuilder<PlatformUser> builder)
    {
        builder.ToTable("platform_users");

        builder.HasKey(x => x.Id).HasName("pk_platform_users");

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

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasColumnType("citext");

        builder.Property(x => x.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasColumnType("citext");

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.FirstName)
            .HasColumnName("first_name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.LastName)
            .HasColumnName("last_name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(x => x.Phone)
            .HasColumnName("phone")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired(false);

        builder.Property(x => x.JobTitle)
            .HasColumnName("job_title")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(x => x.EmailVerifiedAt)
            .HasColumnName("email_verified_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.FailedLoginCount)
            .HasColumnName("failed_login_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.LockedUntil)
            .HasColumnName("locked_until")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.PasswordChangedAt)
            .HasColumnName("password_changed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByPlatformUserId)
            .HasColumnName("updated_by_platform_user_id")
            .IsRequired(false);

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByPlatformUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_platform_users_created_by_platform_user_id_platform_users");

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByPlatformUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_platform_users_updated_by_platform_user_id_platform_users");

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("uq_platform_users_email");

        builder.HasIndex(x => x.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("uq_platform_users_normalized_email");

        builder.ToTable(t => t.HasCheckConstraint("ck_platform_users_status", "status IN ('ACTIVE', 'INACTIVE', 'LOCKED', 'DELETED')"));
    }
}
