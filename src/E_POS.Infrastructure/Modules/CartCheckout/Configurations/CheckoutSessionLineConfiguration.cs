using E_POS.Domain.Modules.CartCheckout.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CartCheckout.Configurations;

public sealed class CheckoutSessionLineConfiguration : IEntityTypeConfiguration<CheckoutSessionLine>
{
    public void Configure(EntityTypeBuilder<CheckoutSessionLine> builder)
    {
        builder.ToTable("checkout_session_lines");

        builder.HasKey(x => x.Id).HasName("pk_checkout_session_lines");

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

        builder.Property(x => x.CheckoutSessionId)
            .HasColumnName("checkout_session_id")
            .IsRequired(false);

        builder.Property(x => x.LineNumber)
            .HasColumnName("line_number")
            .HasColumnType("varchar(80)")
            .HasMaxLength(80);

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4);

        builder.HasOne<CheckoutSession>()
            .WithMany()
            .HasForeignKey(x => x.CheckoutSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_session_lines_checkout_session_id_checkout_sessions");

        builder.HasIndex(x => new { x.CheckoutSessionId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("uq_checkout_session_lines_checkout_session_id_line_number");

        builder.ToTable(t => t.HasCheckConstraint("ck_checkout_session_lines_quantity", "quantity > 0")); 
    }
}

