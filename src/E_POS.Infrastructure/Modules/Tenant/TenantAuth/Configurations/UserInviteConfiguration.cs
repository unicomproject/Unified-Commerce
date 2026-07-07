using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.TenantAuth.Configurations;

public sealed class UserInviteConfiguration : IEntityTypeConfiguration<UserInvite>
{
    public void Configure(EntityTypeBuilder<UserInvite> builder)
    {
        builder.ToTable("user_invites");

        builder.HasKey(x => x.Id).HasName("pk_user_invites");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.InvitedEmail).HasColumnName("invited_email").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(x => x.NormalizedInvitedEmail).HasColumnName("normalized_invited_email").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(x => x.InvitedPhone).HasColumnName("invited_phone").HasColumnType("varchar(40)").HasMaxLength(40);
        builder.Property(x => x.NormalizedInvitedPhone).HasColumnName("normalized_invited_phone").HasColumnType("varchar(40)").HasMaxLength(40);
        
        builder.Property(x => x.AcceptedTenantUserId).HasColumnName("accepted_tenant_user_id");
        builder.Property(x => x.InitialRoleId).HasColumnName("initial_role_id");
        builder.Property(x => x.InitialOutletId).HasColumnName("initial_outlet_id");
        
        builder.Property(x => x.InviteTokenHash).HasColumnName("invite_token_hash").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(x => x.InviteStatus).HasColumnName("invite_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        
        builder.Property(x => x.InvitedByTenantUserId).HasColumnName("invited_by_tenant_user_id");
        builder.Property(x => x.InvitedByPlatformUserId).HasColumnName("invited_by_platform_user_id");
        
        builder.Property(x => x.SentAt).HasColumnName("sent_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.LastSentAt).HasColumnName("last_sent_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ResendCount).HasColumnName("resend_count").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.AcceptedAt).HasColumnName("accepted_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasColumnType("timestamp with time zone");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_user_invites_tenant_id_tenants");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.AcceptedTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_user_invites_accepted_tenant_user_id_tenant_users");

        builder.HasOne<TenantRole>()
            .WithMany()
            .HasForeignKey(x => x.InitialRoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_user_invites_initial_role_id_tenant_roles");

        // Assuming Outlet is not mapped yet or using object, keeping it simple as we did before.
        // In EF Core, if we don't have the navigation property for Outlet, we just define the column properties.
        // Let's add the index for invite_token_hash

        builder.HasIndex(x => x.InviteTokenHash)
            .IsUnique()
            .HasDatabaseName("uq_user_invites_invite_token_hash");

        builder.HasIndex(x => new { x.TenantId, x.NormalizedInvitedEmail })
            .HasFilter("invite_status IN ('PENDING', 'SENT')")
            .IsUnique()
            .HasDatabaseName("uq_user_invites_tenant_id_normalized_invited_email");
    }
}




