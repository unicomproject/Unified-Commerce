using E_POS.Domain.Modules.ReturnExchange.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ReturnExchange.Configurations;

public sealed class ReturnReasonConfiguration : IEntityTypeConfiguration<ReturnReason>
{
    public void Configure(EntityTypeBuilder<ReturnReason> builder)
    {
        builder.ToTable("return_reasons");

        builder.HasKey(x => x.Id).HasName("pk_return_reasons");

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
            .HasColumnName("tenant_id");

        builder.Property(x => x.ReasonCode)
            .HasColumnName("reason_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.AppliesTo)
            .HasColumnName("applies_to")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.HasIndex(x => new { x.TenantId, x.ReasonCode })
            .IsUnique()
            .HasDatabaseName("uq_return_reasons_tenant_id_reason_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_return_reasons_applies_to", "applies_to IN ('RETURN', 'EXCHANGE', 'BOTH')")); 
    }
}

