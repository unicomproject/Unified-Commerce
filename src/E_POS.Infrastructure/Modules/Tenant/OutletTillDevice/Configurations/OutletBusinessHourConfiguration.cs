using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Configurations;

public sealed class OutletBusinessHourConfiguration : IEntityTypeConfiguration<OutletBusinessHour>
{
    public void Configure(EntityTypeBuilder<OutletBusinessHour> builder)
    {
        builder.ToTable("outlet_business_hours");
        builder.HasKey(x => x.Id).HasName("pk_outlet_business_hours");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.OutletId).HasColumnName("outlet_id").IsRequired();
        builder.Property(x => x.DayOfWeek).HasColumnName("day_of_week").HasColumnType("smallint").IsRequired();
        builder.Property(x => x.OpeningTime).HasColumnName("opening_time").HasColumnType("time without time zone").IsRequired(false);
        builder.Property(x => x.ClosingTime).HasColumnName("closing_time").HasColumnType("time without time zone").IsRequired(false);
        builder.Property(x => x.IsClosed).HasColumnName("is_closed").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnName("valid_from").HasColumnType("date").IsRequired(false);
        builder.Property(x => x.ValidUntil).HasColumnName("valid_until").HasColumnType("date").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_outlet_business_hours_tenant_id_tenants");
        builder.HasOne<Outlet>().WithMany().HasForeignKey(x => x.OutletId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_outlet_business_hours_outlet_id_outlets");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_outlet_business_hours_day_of_week", "day_of_week BETWEEN 0 AND 6");
            t.HasCheckConstraint("ck_outlet_business_hours_validity", "valid_until IS NULL OR valid_from IS NULL OR valid_until >= valid_from");
        });
    }
}
