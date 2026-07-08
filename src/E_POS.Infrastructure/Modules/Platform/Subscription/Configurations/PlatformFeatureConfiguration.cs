using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class PlatformFeatureConfiguration : IEntityTypeConfiguration<PlatformFeature>
{
    public void Configure(EntityTypeBuilder<PlatformFeature> builder)
    {
        builder.ToTable("platform_features");

        builder.HasKey(x => x.Id).HasName("pk_platform_features");

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

        builder.Property(x => x.FeatureCode)
            .HasColumnName("feature_code")
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

        builder.Property(x => x.PlatformModuleId)
            .HasColumnName("platform_module_id")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne<PlatformModule>()
            .WithMany()
            .HasForeignKey(x => x.PlatformModuleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_features_platform_module_id_platform_modules");

        builder.HasIndex(x => new { x.PlatformModuleId, x.FeatureCode })
            .IsUnique()
            .HasDatabaseName("uq_platform_features_platform_module_id_feature_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_platform_features_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')")); 
    }
}



