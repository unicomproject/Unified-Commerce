using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.HardwareCash.Entities;
using E_POS.Domain.Modules.Orders.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Domain.Modules.POSOperations.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.POSOperations.Configurations;

public sealed class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("receipts");

        builder.HasKey(x => x.Id).HasName("pk_receipts");

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

        builder.Property(x => x.DocumentNumberSequenceId)
            .HasColumnName("document_number_sequence_id");

        builder.Property(x => x.ReceiptNumber)
            .HasColumnName("receipt_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.SalesOrderId)
            .HasColumnName("sales_order_id")
            .IsRequired();

        builder.Property(x => x.ReceiptType)
            .HasColumnName("receipt_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ReceiptStatus)
            .HasColumnName("receipt_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id")
            .IsRequired();

        builder.Property(x => x.TillId)
            .HasColumnName("till_id")
            .IsRequired();

        builder.Property(x => x.TillSessionId)
            .HasColumnName("till_session_id")
            .IsRequired();

        builder.Property(x => x.BusinessDate)
            .HasColumnName("business_date")
            .IsRequired();

        builder.Property(x => x.IssuedAt)
            .HasColumnName("issued_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.IssuedByTenantUserId)
            .HasColumnName("issued_by_tenant_user_id")
            .IsRequired();

        builder.Property(x => x.ReceiptTemplateVersionId)
            .HasColumnName("receipt_template_version_id");

        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.SubtotalAmount)
            .HasColumnName("subtotal_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.TaxAmount)
            .HasColumnName("tax_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.ChargeAmount)
            .HasColumnName("charge_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.RoundingAmount)
            .HasColumnName("rounding_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.PaidAmount)
            .HasColumnName("paid_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.ChangeAmount)
            .HasColumnName("change_amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.ReceiptDataJson)
            .HasColumnName("receipt_data_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipts_tenant_id_tenants");

        builder.HasOne<SalesOrder>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesOrderId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipts_sales_order_id_sales_orders");

        builder.HasOne<DocumentNumberSequence>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.DocumentNumberSequenceId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipts_document_number_sequence_id_document_number_sequences");

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.OutletId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipts_outlet_id_outlets");

        builder.HasOne<Till>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TillId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipts_till_id_tills");

        builder.HasOne<TillSession>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TillSessionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipts_till_session_id_till_sessions");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.IssuedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipts_issued_by_tenant_user_id_tenant_users");

        builder.HasOne<ReceiptTemplateVersion>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReceiptTemplateVersionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipts_receipt_template_version_id_receipt_template_versions");

        builder.HasIndex(x => new { x.TenantId, x.ReceiptNumber })
            .IsUnique()
            .HasDatabaseName("uq_receipts_tenant_id_receipt_number");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_receipts_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_receipts_receipt_type", "receipt_type IN ('SALE', 'REFUND', 'EXCHANGE', 'REPRINT')");
            t.HasCheckConstraint("ck_receipts_receipt_status", "receipt_status IN ('ISSUED', 'VOIDED', 'CANCELLED')");
            t.HasCheckConstraint("ck_receipts_subtotal_amount", "subtotal_amount >= 0");
            t.HasCheckConstraint("ck_receipts_discount_amount", "discount_amount >= 0");
            t.HasCheckConstraint("ck_receipts_tax_amount", "tax_amount >= 0");
            t.HasCheckConstraint("ck_receipts_charge_amount", "charge_amount >= 0");
            t.HasCheckConstraint("ck_receipts_total_amount", "total_amount >= 0");
            t.HasCheckConstraint("ck_receipts_paid_amount", "paid_amount >= 0");
            t.HasCheckConstraint("ck_receipts_change_amount", "change_amount >= 0");
        });
    }
}
