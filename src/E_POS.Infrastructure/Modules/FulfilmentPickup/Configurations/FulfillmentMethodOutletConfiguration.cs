using E_POS.Domain.Modules.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
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

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.FulfillmentMethodId)
            .HasColumnName("fulfillment_method_id")
            .IsRequired();

        builder.HasOne<FulfillmentMethod>()
            .WithMany()
            .HasForeignKey(x => x.FulfillmentMethodId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_method_outlets_fulfillment_method_id_fulfillment_methods");

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => x.OutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fulfillment_method_outlets_outlet_id_outlets");

        builder.HasIndex(x => new { x.FulfillmentMethodId, x.OutletId })
            .IsUnique()
            .HasDatabaseName("uq_fulfillment_method_outlets_fulfillment_method_id_outlet_id");
    }
}

