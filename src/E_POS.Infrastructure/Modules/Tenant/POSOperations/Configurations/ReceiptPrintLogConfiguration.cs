using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Configurations;

public sealed class ReceiptPrintLogConfiguration : IEntityTypeConfiguration<ReceiptPrintLog>
{
    public void Configure(EntityTypeBuilder<ReceiptPrintLog> builder)
    {
        builder.ToTable("receipt_print_logs");

        builder.HasKey(x => x.Id).HasName("pk_receipt_print_logs");

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

        builder.Property(x => x.ReceiptId)
            .HasColumnName("receipt_id")
            .IsRequired();

        builder.Property(x => x.AttemptNumber)
            .HasColumnName("attempt_number")
            .IsRequired();

        builder.Property(x => x.PrinterDeviceId)
            .HasColumnName("printer_device_id");

        builder.Property(x => x.PrintedCopyType)
            .HasColumnName("printed_copy_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.PrintStatus)
            .HasColumnName("print_status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.PrintedAt)
            .HasColumnName("printed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.OperatorTenantUserId)
            .HasColumnName("operator_tenant_user_id");

        builder.Property(x => x.ErrorCode)
            .HasColumnName("error_code")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("text");

        builder.Property(x => x.PrintResultJson)
            .HasColumnName("print_result_json")
            .HasColumnType("jsonb");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_print_logs_tenant_id_tenants");

        builder.HasOne<Receipt>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReceiptId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_print_logs_receipt_id_receipts");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.OperatorTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_receipt_print_logs_operator_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.ReceiptId, x.AttemptNumber })
            .IsUnique()
            .HasDatabaseName("uq_receipt_print_logs_receipt_id_attempt_number");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_receipt_print_logs_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_receipt_print_logs_attempt_number", "attempt_number > 0");
            t.HasCheckConstraint("ck_receipt_print_logs_printed_copy_type", "printed_copy_type IN ('CUSTOMER_COPY', 'MERCHANT_COPY', 'DUPLICATE_COPY')");
            t.HasCheckConstraint("ck_receipt_print_logs_print_status", "print_status IN ('PENDING', 'PRINTED', 'FAILED', 'CANCELLED')");
        });
    }
}



