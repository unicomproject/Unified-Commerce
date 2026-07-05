using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.Orders.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Orders.Configurations;

public sealed class DocumentNumberSequenceConfiguration : IEntityTypeConfiguration<DocumentNumberSequence>
{
    public void Configure(EntityTypeBuilder<DocumentNumberSequence> builder)
    {
        builder.ToTable("document_number_sequences");

        builder.HasKey(x => x.Id).HasName("pk_document_number_sequences");

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

        builder.Property(x => x.OutletId)
            .HasColumnName("outlet_id");

        builder.Property(x => x.DocumentType)
            .HasColumnName("document_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.DocumentSubtype)
            .HasColumnName("document_subtype")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.CurrentValue)
            .HasColumnName("current_value")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.PaddingLength)
            .HasColumnName("padding_length")
            .IsRequired()
            .HasDefaultValue(6);

        builder.Property(x => x.SalesChannelId)
            .HasColumnName("sales_channel_id");

        builder.Property(x => x.Prefix)
            .HasColumnName("prefix")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Suffix)
            .HasColumnName("suffix")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.ResetRule)
            .HasColumnName("reset_rule")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.LastResetAt)
            .HasColumnName("last_reset_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.LastGeneratedAt)
            .HasColumnName("last_generated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.CreatedByTenantUserId)
            .HasColumnName("created_by_tenant_user_id");

        builder.Property(x => x.UpdatedByTenantUserId)
            .HasColumnName("updated_by_tenant_user_id");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_document_number_sequences_tenant_id_tenants");

        builder.HasOne<SalesChannel>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SalesChannelId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_document_number_sequences_sales_channel_id_sales_channels");

        builder.HasOne<Outlet>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.OutletId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_document_number_sequences_outlet_id_outlets");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_document_number_sequences_created_by_tenant_user_id_tenant_users");

        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByTenantUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_document_number_sequences_updated_by_tenant_user_id_tenant_users");

        builder.HasIndex(x => new { x.TenantId, x.DocumentType, x.DocumentSubtype, x.SalesChannelId, x.OutletId })
            .IsUnique()
            .HasDatabaseName("uq_document_number_sequences_tenant_id_document_type_document_subtype_sales_channel_id_outlet_id");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasDatabaseName("uq_document_number_sequences_tenant_id_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_document_number_sequences_padding_length", "padding_length > 0");
            t.HasCheckConstraint("ck_document_number_sequences_current_value", "current_value >= 0");
            t.HasCheckConstraint("ck_document_number_sequences_row_version", "row_version >= 0");
            t.HasCheckConstraint("ck_document_number_sequences_document_type", "document_type IN ('SALES_ORDER', 'RECEIPT', 'PAYMENT', 'RETURN', 'REFUND', 'EXCHANGE', 'FULFILLMENT', 'PICKUP', 'INVOICE', 'CREDIT_NOTE')");
            t.HasCheckConstraint("ck_document_number_sequences_reset_rule", "reset_rule IN ('NONE', 'DAILY', 'MONTHLY', 'YEARLY')");
            t.HasCheckConstraint("ck_document_number_sequences_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        });
    }
}
