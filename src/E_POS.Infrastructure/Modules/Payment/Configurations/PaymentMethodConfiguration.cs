using E_POS.Domain.Modules.Payment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.Payment.Configurations;

public sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("payment_methods");

        builder.HasKey(x => x.Id).HasName("pk_payment_methods");

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

        builder.Property(x => x.MethodCode)
            .HasColumnName("method_code")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.MethodType)
            .HasColumnName("method_type")
            .HasColumnType("varchar(40)")
            .HasMaxLength(40);

        builder.HasIndex(x => new { x.TenantId, x.MethodCode })
            .IsUnique()
            .HasDatabaseName("uq_payment_methods_tenant_id_method_code");

        builder.ToTable(t => t.HasCheckConstraint("ck_payment_methods_method_type", "method_type IN ('CASH', 'CARD', 'QR', 'BANK_TRANSFER', 'MANUAL', 'OTHER')")); 
    }
}

