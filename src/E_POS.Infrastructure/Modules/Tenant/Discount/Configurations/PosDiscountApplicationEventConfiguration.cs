using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Discount.Configurations;

public sealed class PosDiscountApplicationEventConfiguration : IEntityTypeConfiguration<PosDiscountApplicationEvent>
{
    public void Configure(EntityTypeBuilder<PosDiscountApplicationEvent> builder)
    {
        builder.ToTable("pos_discount_application_events");
        builder.HasKey(x => x.Id).HasName("pk_pos_discount_application_events");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.PosDiscountApplicationId).HasColumnName("pos_discount_application_id").IsRequired();
        builder.Property(x => x.EventType).HasColumnName("event_type").HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.FromStatus).HasColumnName("from_status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ToStatus).HasColumnName("to_status").HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ActorTenantUserId).HasColumnName("actor_tenant_user_id").IsRequired();
        builder.Property(x => x.Note).HasColumnName("note").HasColumnType("text");
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.PosDiscountApplicationId, x.OccurredAt }).HasDatabaseName("ix_pos_discount_application_events_application");
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PosDiscountApplication>().WithMany().HasForeignKey(x => new { x.TenantId, x.PosDiscountApplicationId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TenantUser>().WithMany().HasForeignKey(x => x.ActorTenantUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
