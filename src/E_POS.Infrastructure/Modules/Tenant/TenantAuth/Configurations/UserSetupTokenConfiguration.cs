using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.TenantAuth.Configurations;

public sealed class UserSetupTokenConfiguration : IEntityTypeConfiguration<UserSetupToken>
{
    public void Configure(EntityTypeBuilder<UserSetupToken> builder)
    {
        builder.ToTable("user_setup_tokens");

        builder.HasKey(x => x.Id).HasName("pk_user_setup_tokens");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.TokenHash).HasColumnName("token_hash").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Purpose).HasColumnName("purpose").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UsedAt).HasColumnName("used_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_user_setup_tokens_tenant_id_tenants");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_user_setup_tokens_user_id_tenant_users");

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("uq_user_setup_tokens_token_hash");
    }
}



