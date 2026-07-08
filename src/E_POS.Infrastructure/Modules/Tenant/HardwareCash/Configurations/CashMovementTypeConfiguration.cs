using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Configurations;

public sealed class CashMovementTypeConfiguration : IEntityTypeConfiguration<CashMovementType>
{
    public void Configure(EntityTypeBuilder<CashMovementType> builder)
    {
        builder.ToTable("cash_movement_types");

        builder.HasKey(x => x.Id).HasName("pk_cash_movement_types");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired(false); // ERD: NULL can support system/default types.
        builder.Property(x => x.MovementTypeCode).HasColumnName("movement_type_code").HasColumnType("varchar(60)").HasMaxLength(60).IsRequired();
        builder.Property(x => x.MovementTypeName).HasColumnName("movement_type_name").HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Direction).HasColumnName("direction").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.AffectsExpectedCash).HasColumnName("affects_expected_cash").IsRequired();
        builder.Property(x => x.RequiresReason).HasColumnName("requires_reason").IsRequired();
        builder.Property(x => x.IsSystemType).HasColumnName("is_system_type").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cash_movement_types_tenant_id_tenants");

        builder.HasIndex(x => new { x.TenantId, x.MovementTypeCode }).IsUnique().HasDatabaseName("uq_cash_movement_types_tenant_id_movement_type_code");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_cash_movement_types_direction", "direction <> ''");
            t.HasCheckConstraint("ck_cash_movement_types_status", "status <> ''");
        });
    }
}



