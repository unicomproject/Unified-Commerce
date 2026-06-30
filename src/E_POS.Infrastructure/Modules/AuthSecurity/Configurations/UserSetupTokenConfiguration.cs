using E_POS.Domain.Modules.AuthSecurity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.AuthSecurity.Configurations;

public sealed class UserSetupTokenConfiguration : IEntityTypeConfiguration<UserSetupToken>
{
    public void Configure(EntityTypeBuilder<UserSetupToken> builder)
    {
        builder.ToTable("user_setup_tokens");

        builder.HasKey(x => x.Id).HasName("pk_user_setup_tokens");

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

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(x => x.UserInviteId)
            .HasColumnName("user_invite_id")
            .IsRequired();

        builder.HasOne<UserInvite>()
            .WithMany()
            .HasForeignKey(x => x.UserInviteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_user_setup_tokens_user_invite_id_user_invites");

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("uq_user_setup_tokens_token_hash");

        builder.ToTable(t => t.HasCheckConstraint("ck_user_setup_tokens_status", "status IN ('PENDING', 'USED', 'EXPIRED', 'REVOKED')")); 
    }
}

