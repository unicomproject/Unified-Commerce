using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.Customer.Configurations;

public class CustomerWishlistConfiguration : IEntityTypeConfiguration<CustomerWishlist>
{
    public void Configure(EntityTypeBuilder<CustomerWishlist> builder)
    {
        builder.ToTable("customer_wishlists");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        // Auditable Entity mapping
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // Relationships
        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);

        // Constraints and indexes
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_customer_wishlists_tenant_id");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("ix_customer_wishlists_customer_id");
        
        // Tenant FK
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_customer_wishlists_tenant_id_tenants")
            .OnDelete(DeleteBehavior.Restrict);

        // Customer FK
        builder.HasOne<E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .HasConstraintName("fk_customer_wishlists_customer_id_customers")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
