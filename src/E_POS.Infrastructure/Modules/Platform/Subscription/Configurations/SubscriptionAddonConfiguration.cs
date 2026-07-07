using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionAddonConfiguration : IEntityTypeConfiguration<SubscriptionAddon>
{
    public void Configure(EntityTypeBuilder<SubscriptionAddon> builder)
    {
        builder.ToTable("subscription_addons");

        builder.HasKey(x => x.Id).HasName("pk_subscription_addons");

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

        builder.Property(x => x.AddonCode)
            .HasColumnName("addon_code")
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

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.PriceAmount)
            .HasColumnName("price_amount")
            .HasPrecision(18, 2);

        builder.HasIndex(x => x.AddonCode)
            .IsUnique()
            .HasDatabaseName("uq_subscription_addons_addon_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_addons_price_amount", "price_amount >= 0")); 

        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_addons_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')")); 
    }
}



