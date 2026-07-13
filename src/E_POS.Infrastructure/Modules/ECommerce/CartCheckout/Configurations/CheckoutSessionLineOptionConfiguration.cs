using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Configurations;

public sealed class CheckoutSessionLineOptionConfiguration : IEntityTypeConfiguration<CheckoutSessionLineOption>
{
    public void Configure(EntityTypeBuilder<CheckoutSessionLineOption> builder)
    {
        builder.ToTable("checkout_session_line_options");

        builder.HasKey(x => x.Id).HasName("pk_checkout_session_line_options");

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

        builder.Property(x => x.CheckoutSessionLineId)
            .HasColumnName("checkout_session_line_id")
            .IsRequired();

        builder.Property(x => x.ChoiceGroupId)
            .HasColumnName("choice_group_id")
            .IsRequired();

        builder.Property(x => x.ChoiceOptionId)
            .HasColumnName("choice_option_id")
            .IsRequired();

        builder.Property(x => x.ChoiceGroupNameSnapshot)
            .HasColumnName("choice_group_name_snapshot")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.ChoiceOptionNameSnapshot)
            .HasColumnName("choice_option_name_snapshot")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.PriceAdjustment)
            .HasColumnName("price_adjustment")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order");

        builder.HasOne<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_session_line_options_tenant_id_tenants");

        builder.HasOne<CheckoutSessionLine>()
            .WithMany()
            .HasForeignKey(x => x.CheckoutSessionLineId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_checkout_session_line_options_checkout_session_line_id_checkout_session_lines");

        builder.HasOne<E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.ChoiceGroup>()
            .WithMany()
            .HasForeignKey(x => x.ChoiceGroupId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_session_line_options_choice_group_id_choice_groups");

        builder.HasOne<E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.ChoiceOption>()
            .WithMany()
            .HasForeignKey(x => x.ChoiceOptionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_checkout_session_line_options_choice_option_id_choice_options");

        builder.ToTable(t => t.HasCheckConstraint("ck_checkout_session_line_options_sort_order", "sort_order IS NULL OR sort_order >= 0")); 
    }
}



