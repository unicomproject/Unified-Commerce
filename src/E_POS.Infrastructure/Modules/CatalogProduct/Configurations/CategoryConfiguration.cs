using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using E_POS.Domain.Modules.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(x => x.Id).HasName("pk_categories");

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

        builder.Property(x => x.DepartmentId)
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(x => x.ParentCategoryId)
            .HasColumnName("parent_category_id")
            .IsRequired(false);

        builder.Property(x => x.CategoryCode)
            .HasColumnName("category_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.CategoryName)
            .HasColumnName("category_name")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.CategorySlug)
            .HasColumnName("category_slug")
            .HasColumnType("varchar(180)")
            .HasMaxLength(180)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

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
            .HasConstraintName("fk_categories_tenant_id_tenants");

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_categories_department_id_departments");

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_categories_parent_category_id_categories");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_categories_created_by_tenant_user_id_tenant_users");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_categories_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.CategoryCode })
            .IsUnique()
            .HasDatabaseName("uq_categories_tenant_id_department_id_category_code");

        builder.HasIndex(x => new { x.TenantId, x.CategorySlug })
            .IsUnique()
            .HasDatabaseName("uq_categories_tenant_id_category_slug");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_categories_tenant_id_id");

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_categories_tenant_id_department_id_id");

        builder.ToTable(t => t.HasCheckConstraint("ck_categories_parent_category_id", "parent_category_id IS NULL OR parent_category_id <> id"));
        builder.ToTable(t => t.HasCheckConstraint("ck_categories_sort_order", "sort_order >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_categories_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')"));
    }
}
