using E_POS.Domain.Modules.HardwareCash.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.HardwareCash.Configurations;

public sealed class CashMovementTypeConfiguration : IEntityTypeConfiguration<CashMovementType>
{
    public void Configure(EntityTypeBuilder<CashMovementType> builder)
    {
        builder.ToTable("cash_movement_types");

        builder.HasKey(x => x.Id).HasName("pk_cash_movement_types");

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

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.MovementTypeCode)
            .HasColumnName("movement_type_code")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.HasIndex(x => new { x.TenantId, x.MovementTypeCode })
            .IsUnique()
            .HasDatabaseName("uq_cash_movement_types_tenant_id_movement_type_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_cash_movement_types_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')")); 
    }
}

