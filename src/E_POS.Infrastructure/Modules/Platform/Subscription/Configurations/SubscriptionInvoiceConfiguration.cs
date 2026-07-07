using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Configurations;

public sealed class SubscriptionInvoiceConfiguration : IEntityTypeConfiguration<SubscriptionInvoice>
{
    public void Configure(EntityTypeBuilder<SubscriptionInvoice> builder)
    {
        builder.ToTable("subscription_invoices");

        builder.HasKey(x => x.Id).HasName("pk_subscription_invoices");

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

        builder.Property(x => x.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.TenantSubscriptionId)
            .HasColumnName("tenant_subscription_id")
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 2);

        builder.Property(x => x.InvoiceStatus)
            .HasColumnName("invoice_status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasDefaultValue("DRAFT");

        builder.Property(x => x.BillingCycle)
            .HasColumnName("billing_cycle")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20);

        builder.Property(x => x.DueAt)
            .HasColumnName("due_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_invoices_tenant_id_tenants");

        builder.HasOne<TenantSubscription>()
            .WithMany()
            .HasForeignKey(x => x.TenantSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_subscription_invoices_tenant_subscription_id_tenant_subscriptions");

        builder.HasIndex(x => new { x.TenantId, x.InvoiceNumber })
            .IsUnique()
            .HasDatabaseName("uq_subscription_invoices_tenant_id_invoice_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_subscription_invoices_total_amount", "total_amount >= 0")); 
    }
}




