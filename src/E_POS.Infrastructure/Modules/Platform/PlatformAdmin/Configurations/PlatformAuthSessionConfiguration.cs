using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Configurations;

public sealed class PlatformAuthSessionConfiguration : IEntityTypeConfiguration<PlatformAuthSession>
{
    public void Configure(EntityTypeBuilder<PlatformAuthSession> builder)
    {
        builder.ToTable("platform_auth_sessions");

        builder.HasKey(x => x.Id).HasName("pk_platform_auth_sessions");

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

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.SessionTokenHash)
            .HasColumnName("session_token_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_auth_sessions_platform_user_id_platform_users");

        builder.HasIndex(x => x.SessionTokenHash)
            .IsUnique()
            .HasDatabaseName("uq_platform_auth_sessions_session_token_hash");

        builder.ToTable(t => t.HasCheckConstraint("ck_platform_auth_sessions_status", "status IN ('ACTIVE', 'EXPIRED', 'REVOKED')")); 
    }
}



