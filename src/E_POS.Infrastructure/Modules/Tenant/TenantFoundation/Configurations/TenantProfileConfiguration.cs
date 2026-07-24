using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Shared.Media.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Configurations;

public sealed class TenantProfileConfiguration : IEntityTypeConfiguration<TenantProfile>
{
    public void Configure(EntityTypeBuilder<TenantProfile> builder)
    {
        builder.ToTable("tenant_profiles");

        builder.HasKey(x => x.Id).HasName("pk_tenant_profiles");

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

        builder.Property(x => x.BusinessTypeId)
            .HasColumnName("business_type_id")
            .IsRequired(false);

        builder.Property(x => x.LegalName)
            .HasColumnName("legal_name")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.TradingName)
            .HasColumnName("trading_name")
            .HasColumnType("varchar(250)")
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(x => x.PrimaryContactName)
            .HasColumnName("primary_contact_name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(x => x.PrimaryEmail)
            .HasColumnName("primary_email")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(x => x.PrimaryPhone)
            .HasColumnName("primary_phone")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired(false);

        builder.Property(x => x.WebsiteUrl)
            .HasColumnName("website_url")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.LogoUrl)
            .HasColumnName("logo_url")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.LogoMediaAssetId)
            .HasColumnName("logo_media_asset_id")
            .IsRequired(false);

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.CreatedByPlatformUserId)
            .HasColumnName("created_by_platform_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByPlatformUserId)
            .HasColumnName("updated_by_platform_user_id")
            .IsRequired(false);

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_tenant_profiles_tenant_id_tenants");

        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.LogoMediaAssetId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_profiles_logo_media_asset_tenant");

        builder.HasIndex(x => new { x.TenantId, x.LogoMediaAssetId })
            .HasDatabaseName("ix_tenant_profiles_tenant_id_logo_media_asset_id");

        builder.HasIndex(x => x.TenantId)
            .IsUnique()
            .HasDatabaseName("uq_tenant_profiles_tenant_id");
    }
}
