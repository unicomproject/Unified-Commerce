using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Orders.Configurations;

public sealed class SalesOrderNumberSequenceConfiguration : IEntityTypeConfiguration<SalesOrderNumberSequence>
{
    public void Configure(EntityTypeBuilder<SalesOrderNumberSequence> builder)
    {
        builder.ToTable("sales_order_number_sequences");

        builder.HasKey(x => x.Id).HasName("pk_sales_order_number_sequences");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.SalesChannel).HasColumnName("sales_channel").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.OrderType).HasColumnName("order_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Prefix).HasColumnName("prefix").HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.CurrentValue).HasColumnName("current_value").IsRequired();
        builder.Property(x => x.ResetRule).HasColumnName("reset_rule").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        builder.Property(x => x.LastGeneratedAt).HasColumnName("last_generated_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_sales_order_number_sequences_tenant_id_tenants");

        builder.HasIndex(x => new { x.TenantId, x.SalesChannel, x.OrderType })
            .IsUnique()
            .HasDatabaseName("uq_sales_order_number_sequences_tenant_id_sales_channel_order_type");
    }
}


