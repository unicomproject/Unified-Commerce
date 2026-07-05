using E_POS.Domain.Modules.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.FulfilmentPickup.Configurations;

public sealed class FulfillmentMethodOutletConfiguration : IEntityTypeConfiguration<FulfillmentMethodOutlet>
{
    public void Configure(EntityTypeBuilder<FulfillmentMethodOutlet> builder)
    {
        builder.ToTable("fulfillment_method_outlets");

        builder.HasKey(x => x.Id).HasName("pk_fulfillment_method_outlets");

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

        builder.Property(x => x.FulfillmentMethodId)
            .HasColumnName("fulfillment_method_id")
            .IsRequired();

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id")
            .IsRequired();

        builder.Property(x => x.PreparationLeadMinutes)
            .HasColumnName("preparation_lead_minutes")
            .IsRequired(false);

        builder.Property(x => x.PickupWindowMinutes)
            .HasColumnName("pickup_window_minutes")
            .IsRequired(false);

        builder.Property(x => x.CutoffTime)
            .HasColumnName("cutoff_time")
            .HasColumnType("time")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.FulfillmentMethodId, x.OutletId })
            .IsUnique()
            .HasDatabaseName("ux_fulfillment_method_outlets_cf79fc78");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_fulfillment_method_outlets_tenant_id_id");

        builder.HasOne<E_POS.Domain.Modules.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_method_outlets_f359be5f");

        builder.HasOne<E_POS.Domain.Modules.FulfilmentPickup.Entities.FulfillmentMethod>()
            .WithMany()
            .HasForeignKey(x => x.FulfillmentMethodId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_method_outlets_e627f2fa");

        builder.HasOne<E_POS.Domain.Modules.OutletTillDevice.Entities.Outlet>()
            .WithMany()
            .HasForeignKey(x => x.OutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_method_outlets_821f4b29");

        builder.HasOne<E_POS.Domain.Modules.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_method_outlets_bd06d410");

        builder.HasOne<E_POS.Domain.Modules.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_method_outlets_6480bfac");
        // </second-brain-constraints>
    }
}