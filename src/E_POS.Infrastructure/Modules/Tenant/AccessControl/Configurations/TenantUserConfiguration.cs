using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Configurations;

public sealed class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
{
    public void Configure(EntityTypeBuilder<TenantUser> builder)
    {
        builder.ToTable("tenant_users");

        builder.HasKey(x => x.Id).HasName("pk_tenant_users");

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

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.EncryptedPassword)
            .HasColumnName("encrypted_password")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasColumnName("phone")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20);

        builder.Property(x => x.UnmaskedPhone)
            .HasColumnName("unmasked_phone")
            .HasColumnType("varchar(25)")
            .HasMaxLength(25);

        builder.Property(x => x.PasswordSalt)
            .HasColumnName("password_salt")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500);

        builder.Property(x => x.ProfileImageUrl)
            .HasColumnName("profile_image_url");

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id");

        builder.Property(x => x.DefaultOutletId)
            .HasColumnName("default_outlet_id")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.UserType)
            .HasColumnName("user_type")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.AccountStatus)
            .HasColumnName("account_status")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.LockedUntil)
            .HasColumnName("locked_until")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.FailedLoginAttempts)
            .HasColumnName("failed_login_attempts")
            .IsRequired();

        builder.Property(x => x.PasswordChangeRequiredAt)
            .HasColumnName("password_change_required_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.AcceptedPrivacyTerms)
            .HasColumnName("accepted_privacy_terms")
            .IsRequired();

        builder.Property(x => x.AcceptedTermsVersion)
            .HasColumnName("accepted_terms_version")
            .HasColumnType("varchar(10)")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id");

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id");

        builder.Property(x => x.SourceUserType)
            .HasColumnName("source_user_type")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_users_tenant_id_tenants");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tenant_users_created_by");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tenant_users_updated_by");

        builder.HasIndex(x => new { x.TenantId, x.Email })
            .IsUnique()
            .HasDatabaseName("uq_tenant_users_tenant_id_email");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_tenant_users_tenant_id_id");

        builder.HasIndex(x => new { x.TenantId, x.UnmaskedPhone })
            .IsUnique()
            .HasDatabaseName("uq_tenant_users_tenant_id_unmasked_phone")
            .HasFilter("unmasked_phone IS NOT NULL");

        builder.ToTable(t => 
        {
            t.HasCheckConstraint("ck_tenant_users_locked_until", "locked_until IS NULL OR locked_until > now()");
            t.HasCheckConstraint("ck_tenant_users_source_user_type", "source_user_type IN ('admin', 'outlet', 'platform')");
        });
    }
}
