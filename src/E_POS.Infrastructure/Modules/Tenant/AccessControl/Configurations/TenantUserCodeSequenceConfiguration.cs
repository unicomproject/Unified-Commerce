using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Configurations;

public sealed class TenantUserCodeSequenceConfiguration : IEntityTypeConfiguration<TenantUserCodeSequence>
{
    public void Configure(EntityTypeBuilder<TenantUserCodeSequence> builder)
    {
        builder.ToTable("tenant_user_code_sequences");
        builder.HasKey(x => x.Id).HasName("pk_tenant_user_code_sequences");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.SequenceType).HasColumnName("sequence_type").HasColumnType("varchar(64)").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Year).HasColumnName("year").IsRequired();
        builder.Property(x => x.CurrentValue).HasColumnName("current_value").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy); builder.Ignore(x => x.UpdatedBy);
        builder.HasIndex(x => new { x.TenantId, x.SequenceType, x.Year }).IsUnique().HasDatabaseName("uq_tenant_user_code_sequences_scope");
    }
}
