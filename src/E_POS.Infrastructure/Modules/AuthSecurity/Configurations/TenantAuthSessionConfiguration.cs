using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.AuthSecurity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.AuthSecurity.Configurations;

public sealed class TenantAuthSessionConfiguration : IEntityTypeConfiguration<TenantAuthSession>
{
    public void Configure(EntityTypeBuilder<TenantAuthSession> builder)
    {
        builder.ToTable("tenant_auth_sessions");

        builder.HasKey(x => x.Id).HasName("pk_tenant_auth_sessions");

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

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.SessionTokenHash)
            .HasColumnName("session_token_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.TenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_auth_sessions_tenant_user_id_tenant_users");

        builder.HasIndex(x => x.SessionTokenHash)
            .IsUnique()
            .HasDatabaseName("uq_tenant_auth_sessions_session_token_hash");

        builder.ToTable(t => t.HasCheckConstraint("ck_tenant_auth_sessions_status", "status IN ('ACTIVE', 'EXPIRED', 'REVOKED')")); 
    }
}

