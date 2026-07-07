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

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("uq_platform_users_email");

        builder.HasIndex(x => x.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("uq_platform_users_normalized_email");

        builder.ToTable(t => t.HasCheckConstraint("ck_platform_users_status", "status IN ('ACTIVE', 'INACTIVE', 'LOCKED', 'DELETED')"));
    }
}


