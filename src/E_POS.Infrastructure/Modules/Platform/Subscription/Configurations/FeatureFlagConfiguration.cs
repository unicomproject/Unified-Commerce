using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("feature_flags");

        builder.HasKey(x => x.Id).HasName("pk_feature_flags");

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

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.FlagCode)
            .HasColumnName("flag_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.PlatformFeatureId)
            .HasColumnName("platform_feature_id")
            .IsRequired();

        builder.HasOne<PlatformFeature>()
            .WithMany()
            .HasForeignKey(x => x.PlatformFeatureId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_feature_flags_platform_feature_id_platform_features");

        builder.HasIndex(x => x.FlagCode)
            .IsUnique()
            .HasDatabaseName("uq_feature_flags_flag_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_feature_flags_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')")); 
    }
}



