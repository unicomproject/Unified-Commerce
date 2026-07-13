using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Configurations;

public sealed class SalesChannelConfiguration : IEntityTypeConfiguration<SalesChannel>
{
    public void Configure(EntityTypeBuilder<SalesChannel> builder)
    {
        builder.ToTable("sales_channels");

        builder.HasKey(x => x.Id).HasName("pk_sales_channels");

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

        builder.Property(x => x.PlatformSalesChannelId)
            .HasColumnName("platform_sales_channel_id")
            .IsRequired();

        builder.Property(x => x.CustomName)
            .HasColumnName("custom_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_channels_tenant_id_tenants");

        builder.HasOne<E_POS.Domain.Modules.Platform.PlatformFoundation.Entities.PlatformSalesChannel>()
            .WithMany()
            .HasForeignKey(x => x.PlatformSalesChannelId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_channels_platform_sales_channel_id");

        builder.HasIndex(x => new { x.TenantId, x.PlatformSalesChannelId })
            .IsUnique()
            .HasDatabaseName("ix_sales_channels_tenant_id_platform_channel_id");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_sales_channels_status",
            "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_sales_channels_sort_order",
            "sort_order >= 0"));
    }
}
