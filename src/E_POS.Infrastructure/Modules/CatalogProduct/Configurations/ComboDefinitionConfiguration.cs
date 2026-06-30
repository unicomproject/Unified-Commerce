using E_POS.Domain.Modules.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Configurations;

public sealed class ComboDefinitionConfiguration : IEntityTypeConfiguration<ComboDefinition>
{
    public void Configure(EntityTypeBuilder<ComboDefinition> builder)
    {
        builder.ToTable("combo_definitions");

        builder.HasKey(x => x.Id).HasName("pk_combo_definitions");

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
            .HasColumnName("tenant_id");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.ComboCode)
            .HasColumnName("combo_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_combo_definitions_product_id_products");

        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.ComboCode })
            .IsUnique()
            .HasDatabaseName("uq_combo_definitions_tenant_id_product_id_combo_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_combo_definitions_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')")); 
    }
}

