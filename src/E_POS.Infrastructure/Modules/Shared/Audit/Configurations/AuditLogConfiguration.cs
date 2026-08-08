using E_POS.Domain.Modules.Shared.Audit.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Shared.Audit.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id).HasName("pk_audit_logs");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired(false);

        builder.Property(x => x.ActorUserId)
            .HasColumnName("actor_user_id")
            .IsRequired(false);

        builder.Property(x => x.ActorType)
            .HasColumnName("actor_type")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasColumnName("entity_type")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id")
            .IsRequired(false);

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.OldValues)
            .HasColumnName("old_values")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.NewValues)
            .HasColumnName("new_values")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.Ip)
            .HasColumnName("ip")
            .HasColumnType("varchar(45)")
            .IsRequired(false);

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
