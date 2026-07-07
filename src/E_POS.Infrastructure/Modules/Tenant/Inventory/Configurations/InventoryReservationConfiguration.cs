using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Customer.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Configurations;

public sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("inventory_reservations");

        builder.HasKey(x => x.Id).HasName("pk_inventory_reservations");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by_tenant_user_id").IsRequired(false);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by_tenant_user_id").IsRequired(false);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ReservationNumber).HasColumnName("reservation_number").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.ReservationSource).HasColumnName("reservation_source").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.SourceReferenceId).HasColumnName("source_reference_id").IsRequired(false);
        builder.Property(x => x.SourceReferenceNumber).HasColumnName("source_reference_number").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.SalesChannelId).HasColumnName("sales_channel_id").IsRequired(false);
        builder.Property(x => x.FulfillmentOutletId).HasColumnName("fulfillment_outlet_id").IsRequired(false);
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired(false);
        builder.Property(x => x.ReservationStatus).HasColumnName("reservation_status").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ReservedAt).HasColumnName("reserved_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.ReleasedAt).HasColumnName("released_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.ReleaseReason).HasColumnName("release_reason").HasColumnType("varchar(250)").HasMaxLength(250).IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reservations_tenant_id_tenants");
        
        builder.HasOne<SalesChannel>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesChannelId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reservations_sales_channel_id_sales_channels");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => new { x.TenantId, x.FulfillmentOutletId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reservations_fulfillment_outlet_id_outlets");
        builder.HasOne<E_POS.Domain.Modules.Customer.Entities.Customer>().WithMany().HasForeignKey(x => new { x.TenantId, x.CustomerId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reservations_customer_id_customers");
        
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reservations_created_by_tenant_user_id_tenant_users");
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_inventory_reservations_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.ReservationNumber }).IsUnique().HasDatabaseName("uq_inventory_reservations_tenant_id_reservation_number");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_inventory_reservations_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_inventory_reservations_expires_at", "expires_at IS NULL OR expires_at >= reserved_at");
            t.HasCheckConstraint("ck_inventory_reservations_released_at", "released_at IS NULL OR released_at >= reserved_at");
        });
    }
}


