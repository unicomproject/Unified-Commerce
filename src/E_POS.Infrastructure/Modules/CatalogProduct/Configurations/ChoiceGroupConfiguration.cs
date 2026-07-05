using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class ChoiceGroupConfiguration : IEntityTypeConfiguration<ChoiceGroup>
{
    public void Configure(EntityTypeBuilder<ChoiceGroup> builder)
    {
        builder.ToTable("choice_groups");

        builder.HasKey(x => x.Id).HasName("pk_choice_groups");

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

        builder.Property(x => x.GroupCode)
            .HasColumnName("group_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.GroupName)
            .HasColumnName("group_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.MinSelect)
            .HasColumnName("min_select")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.MaxSelect)
            .HasColumnName("max_select")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id")
            .IsRequired(false);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_choice_groups_tenant_id_tenants");

        builder.HasIndex(x => new { x.TenantId, x.GroupCode })
            .IsUnique()
            .HasDatabaseName("uq_choice_groups_tenant_id_group_code");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_choice_groups_tenant_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_choice_groups_min_select", "min_select >= 0")); 
        builder.ToTable(t => t.HasCheckConstraint("ck_choice_groups_max_select", "max_select > 0")); 
        builder.ToTable(t => t.HasCheckConstraint("ck_choice_groups_max_select_min_select", "max_select >= min_select")); 
        builder.ToTable(t => t.HasCheckConstraint("ck_choice_groups_sort_order", "sort_order >= 0")); 
        builder.ToTable(t => t.HasCheckConstraint("ck_choice_groups_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')")); 
    }
}
