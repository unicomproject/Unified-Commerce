using E_POS.Domain.Modules.OutletTillDevice.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.OutletTillDevice.Configurations;

public sealed class OutletAddressConfiguration : IEntityTypeConfiguration<OutletAddress>
{
    public void Configure(EntityTypeBuilder<OutletAddress> builder)
    {
        builder.ToTable("outlet_addresses");

        builder.HasKey(x => x.Id).HasName("pk_outlet_addresses");

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

        builder.Property(x => x.AddressType)
            .HasColumnName("address_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => x.OutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_outlet_addresses_outlet_id_outlets");

        builder.ToTable(t => t.HasCheckConstraint("ck_outlet_addresses_address_type", "address_type IN ('PHYSICAL', 'BILLING', 'PICKUP')")); 
    }
}

