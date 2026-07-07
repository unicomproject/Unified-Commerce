using E_POS.Domain.Modules.Tenant.Payment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.Payment.Configurations;

public sealed class SalesPaymentTransactionConfiguration : IEntityTypeConfiguration<SalesPaymentTransaction>
{
    public void Configure(EntityTypeBuilder<SalesPaymentTransaction> builder)
    {
        builder.ToTable("sales_payment_transactions");

        builder.HasKey(x => x.Id).HasName("pk_sales_payment_transactions");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(x => x.UpdatedAt);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.SalesPaymentId)
            .HasColumnName("sales_payment_id")
            .IsRequired();

        builder.Property(x => x.ParentTransactionId)
            .HasColumnName("parent_transaction_id")
            .IsRequired(false);

        builder.Property(x => x.TransactionType)
            .HasColumnName("transaction_type")
            .IsRequired();

        builder.Property(x => x.TransactionStatus)
            .HasColumnName("transaction_status")
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.ExternalTransactionReference)
            .HasColumnName("external_transaction_reference")
            .HasColumnType("varchar(180)")
            .HasMaxLength(180)
            .IsRequired(false);

        builder.Property(x => x.ProviderName)
            .HasColumnName("provider_name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.ProviderResponseJson)
            .HasColumnName("provider_response_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.ProcessedByTenantUserId)
            .HasColumnName("processed_by_tenant_user_id")
            .IsRequired(false);

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // <second-brain-constraints>
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_sales_payment_transactions_e759526b");

        builder.HasIndex(x => new { x.TenantId, x.ProviderName, x.ExternalTransactionReference })
            .IsUnique()
            .HasDatabaseName("ux_sales_payment_transactions_5562416e");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payment_transactions_d1461364");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Payment.Entities.SalesPayment>()
            .WithMany()
            .HasForeignKey(x => x.SalesPaymentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payment_transactions_b80a12d3");

        builder.HasOne<E_POS.Domain.Modules.Tenant.Payment.Entities.SalesPaymentTransaction>()
            .WithMany()
            .HasForeignKey(x => x.ParentTransactionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payment_transactions_d36a2128");

        builder.HasOne<E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.ProcessedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sales_payment_transactions_31a79680");
        // <second-brain-checks>
        builder.ToTable(t => t.HasCheckConstraint("ck_sales_payment_transactions_d53c5618", "amount > 0"));
        // </second-brain-checks>

        // </second-brain-constraints>
    }
}

