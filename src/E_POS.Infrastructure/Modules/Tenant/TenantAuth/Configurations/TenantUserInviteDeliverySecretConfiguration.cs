using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.TenantAuth.Configurations;

public sealed class TenantUserInviteDeliverySecretConfiguration : IEntityTypeConfiguration<TenantUserInviteDeliverySecret>
{
    public void Configure(EntityTypeBuilder<TenantUserInviteDeliverySecret> builder)
    {
        builder.ToTable("tenant_user_invite_delivery_secrets");
        builder.HasKey(x => x.Id).HasName("pk_tenant_user_invite_delivery_secrets");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.TenantUserId).HasColumnName("tenant_user_id").IsRequired();
        builder.Property(x => x.InviteId).HasColumnName("invite_id").IsRequired();
        builder.Property(x => x.EncryptedToken).HasColumnName("encrypted_token").HasColumnType("text").IsRequired();
        builder.Property(x => x.KeyVersion).HasColumnName("key_version").HasColumnType("varchar(64)").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.PurgedAt).HasColumnName("purged_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy); builder.Ignore(x => x.UpdatedBy);
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.TenantUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tenant_user_invite_delivery_secrets_user");
        builder.HasOne<UserInvite>().WithMany().HasForeignKey(x => x.InviteId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tenant_user_invite_delivery_secrets_invite");
        builder.HasIndex(x => x.InviteId).IsUnique().HasDatabaseName("uq_tenant_user_invite_delivery_secrets_invite_id");
        builder.HasIndex(x => new { x.TenantId, x.TenantUserId }).HasDatabaseName("ix_tenant_user_invite_delivery_secrets_target");
        builder.HasIndex(x => new { x.PurgedAt, x.ExpiresAt, x.CreatedAt })
            .HasDatabaseName("ix_tenant_user_invite_delivery_secrets_cleanup");
    }
}
