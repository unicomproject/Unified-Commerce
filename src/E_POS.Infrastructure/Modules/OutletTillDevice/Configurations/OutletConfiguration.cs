using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.OutletTillDevice.Configurations;

public sealed class OutletConfiguration : IEntityTypeConfiguration<Outlet>
{
    public void Configure(EntityTypeBuilder<Outlet> builder)
    {
        builder.ToTable("outlets");
        builder.HasKey(x => x.Id).HasName("pk_outlets");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasColumnType("varchar(200)").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30);
        builder.Property(x => x.OutletCode).HasColumnName("outlet_code").HasColumnType("varchar(80)").HasMaxLength(80);
        builder.Property(x => x.OutletType).HasColumnName("outlet_type").HasColumnType("varchar(40)").HasMaxLength(40).HasDefaultValue("STORE").IsRequired();
        builder.Property(x => x.IsOnlineVisible).HasColumnName("is_online_visible").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired(false);
        builder.Property(x => x.ContactEmail).HasColumnName("contact_email").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired(false);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_outlets_tenant_id_tenants");
        builder.HasIndex(x => new { x.TenantId, x.OutletCode }).IsUnique().HasDatabaseName("uq_outlets_tenant_id_outlet_code");
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique().HasDatabaseName("uq_outlets_tenant_id_id");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_outlets_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
            t.HasCheckConstraint("ck_outlets_outlet_type", "outlet_type IN ('STORE', 'WAREHOUSE')");
        });
    }
}