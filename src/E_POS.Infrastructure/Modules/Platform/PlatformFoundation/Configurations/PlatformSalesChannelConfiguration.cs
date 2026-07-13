using E_POS.Domain.Modules.Platform.PlatformFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.PlatformFoundation.Configurations;

public sealed class PlatformSalesChannelConfiguration : IEntityTypeConfiguration<PlatformSalesChannel>
{
    public void Configure(EntityTypeBuilder<PlatformSalesChannel> builder)
    {
        builder.ToTable("platform_sales_channels");

        builder.HasKey(x => x.Id).HasName("pk_platform_sales_channels");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(x => x.ChannelCode)
            .HasColumnName("channel_code")
            .HasColumnType("varchar(50)")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DefaultName)
            .HasColumnName("default_name")
            .HasColumnType("varchar(100)")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ChannelType)
            .HasColumnName("channel_type")
            .HasColumnType("varchar(40)")
            .IsRequired()
            .HasMaxLength(40);

        builder.HasIndex(x => x.ChannelCode)
            .HasDatabaseName("ix_platform_sales_channels_channel_code")
            .IsUnique();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_platform_sales_channels_channel_type", "channel_type IN ('PHYSICAL', 'ONLINE', 'AGGREGATOR', 'B2B', 'OTHER')");
        });
    }
}
