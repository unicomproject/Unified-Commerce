using E_POS.Domain.Modules.OfflineSync.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.OfflineSync.Configurations;

public sealed class OfflineClientConfiguration : IEntityTypeConfiguration<OfflineClient>
{
    public void Configure(EntityTypeBuilder<OfflineClient> builder)
    {
        builder.ToTable("offline_clients");

        builder.HasKey(x => x.Id).HasName("pk_offline_clients");

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
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id")
            .IsRequired(false);

        builder.Property(x => x.PosDeviceId)
            .HasColumnName("pos_device_id")
            .IsRequired(false);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ClientCode)
            .HasColumnName("client_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.MaxOfflineDurationMinutes)
            .HasColumnName("max_offline_duration_minutes");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_clients_tenant_id_tenants");

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => x.OutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_clients_outlet_id_outlets");

        builder.HasOne<PosDevice>()
            .WithMany()
            .HasForeignKey(x => x.PosDeviceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_offline_clients_pos_device_id_pos_devices");

        builder.HasIndex(x => new { x.TenantId, x.ClientCode })
            .IsUnique()
            .HasDatabaseName("uq_offline_clients_tenant_id_client_code");

        builder.HasIndex(x => new { x.TenantId, x.PosDeviceId })
            .IsUnique()
            .HasDatabaseName("uq_offline_clients_tenant_id_pos_device_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_offline_clients_max_offline_duration_minutes", "max_offline_duration_minutes IS NULL OR max_offline_duration_minutes > 0")); 
    }
}

