using E_POS.Domain.Modules.CartCheckout.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.CartCheckout.Configurations;

public sealed class CheckoutSessionLineComponentConfiguration : IEntityTypeConfiguration<CheckoutSessionLineComponent>
{
    public void Configure(EntityTypeBuilder<CheckoutSessionLineComponent> builder)
    {
        builder.ToTable("checkout_session_line_components");

        builder.HasKey(x => x.Id).HasName("pk_checkout_session_line_components");

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

        builder.Property(x => x.CheckoutSessionLineId)
            .HasColumnName("checkout_session_line_id")
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne<CheckoutSessionLine>()
            .WithMany()
            .HasForeignKey(x => x.CheckoutSessionLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_session_line_components_checkout_session_line_id_checkout_session_lines");

        builder.ToTable(t => t.HasCheckConstraint("ck_checkout_session_line_components_quantity", "quantity > 0")); 
    }
}

