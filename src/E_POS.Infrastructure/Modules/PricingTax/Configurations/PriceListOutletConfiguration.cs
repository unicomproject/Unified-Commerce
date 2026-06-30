using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Domain.Modules.PricingTax.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.PricingTax.Configurations;

public sealed class PriceListOutletConfiguration : IEntityTypeConfiguration<PriceListOutlet>
{
    public void Configure(EntityTypeBuilder<PriceListOutlet> builder)
    {
        builder.ToTable("price_list_outlets");

        builder.HasKey(x => x.Id).HasName("pk_price_list_outlets");

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

        builder.Property(x => x.PriceListId)
            .HasColumnName("price_list_id")
            .IsRequired();

        builder.HasOne<PriceList>()
            .WithMany()
            .HasForeignKey(x => x.PriceListId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_price_list_outlets_price_list_id_price_lists");

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => x.OutletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_price_list_outlets_outlet_id_outlets");

        builder.HasIndex(x => new { x.PriceListId, x.OutletId })
            .IsUnique()
            .HasDatabaseName("uq_price_list_outlets_price_list_id_outlet_id");
    }
}

