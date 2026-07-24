using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Step 3 Select Items schema hardening:
/// tenant-composite FKs for returns/images, quantity CHECKs, return-line uniqueness,
/// and outlet/return-reason tenant alternate keys.
/// Held-sale line_status remains ACTIVE (application fix); CHECK unchanged.
/// </summary>
public partial class HardenReturnEligibilityTenantConstraints : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX IF NOT EXISTS uq_outlets_tenant_id_id
                ON outlets (tenant_id, id);

            CREATE UNIQUE INDEX IF NOT EXISTS uq_return_reasons_tenant_id_id
                ON return_reasons (tenant_id, id);

            CREATE UNIQUE INDEX IF NOT EXISTS uq_sales_returns_tenant_id_id
                ON sales_returns (tenant_id, id);

            CREATE UNIQUE INDEX IF NOT EXISTS uq_sales_return_lines_tenant_id_id
                ON sales_return_lines (tenant_id, id);

            CREATE UNIQUE INDEX IF NOT EXISTS uq_product_images_tenant_id_id
                ON product_images (tenant_id, id);

            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS fk_sales_returns_1549e8c2;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS fk_sales_returns_8e3771a4;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS fk_sales_returns_f8fcc58d;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS fk_sales_returns_8bbbda2e;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS fk_sales_returns_8308f645;

            ALTER TABLE sales_returns
                ADD CONSTRAINT fk_sales_returns_document_number_sequence_tenant
                FOREIGN KEY (tenant_id, document_number_sequence_id)
                REFERENCES document_number_sequences (tenant_id, id)
                ON DELETE RESTRICT;

            ALTER TABLE sales_returns
                ADD CONSTRAINT fk_sales_returns_sales_order_tenant
                FOREIGN KEY (tenant_id, sales_order_id)
                REFERENCES sales_orders (tenant_id, id)
                ON DELETE RESTRICT;

            ALTER TABLE sales_returns
                ADD CONSTRAINT fk_sales_returns_customer_tenant
                FOREIGN KEY (tenant_id, customer_id)
                REFERENCES customers (tenant_id, id)
                ON DELETE RESTRICT;

            ALTER TABLE sales_returns
                ADD CONSTRAINT fk_sales_returns_outlet_tenant
                FOREIGN KEY (tenant_id, outlet_id)
                REFERENCES outlets (tenant_id, id)
                ON DELETE RESTRICT;

            ALTER TABLE sales_returns
                ADD CONSTRAINT fk_sales_returns_return_reason_tenant
                FOREIGN KEY (tenant_id, return_reason_id)
                REFERENCES return_reasons (tenant_id, id)
                ON DELETE RESTRICT;

            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS fk_sales_return_lines_6cf63e7f;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS fk_sales_return_lines_1e6282f1;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS fk_sales_return_lines_4427f485;

            ALTER TABLE sales_return_lines
                ADD CONSTRAINT fk_sales_return_lines_return_tenant
                FOREIGN KEY (tenant_id, sales_return_id)
                REFERENCES sales_returns (tenant_id, id)
                ON DELETE RESTRICT;

            ALTER TABLE sales_return_lines
                ADD CONSTRAINT fk_sales_return_lines_order_line_tenant
                FOREIGN KEY (tenant_id, sales_order_line_id)
                REFERENCES sales_order_lines (tenant_id, id)
                ON DELETE RESTRICT;

            ALTER TABLE sales_return_lines
                ADD CONSTRAINT fk_sales_return_lines_return_reason_tenant
                FOREIGN KEY (tenant_id, return_reason_id)
                REFERENCES return_reasons (tenant_id, id)
                ON DELETE RESTRICT;

            CREATE UNIQUE INDEX IF NOT EXISTS uq_sales_return_lines_tenant_return_order_line
                ON sales_return_lines (tenant_id, sales_return_id, sales_order_line_id);

            ALTER TABLE product_images DROP CONSTRAINT IF EXISTS fk_product_images_product_id_products;
            ALTER TABLE product_images DROP CONSTRAINT IF EXISTS fk_product_images_product_variant_id_product_variants;

            ALTER TABLE product_images
                ADD CONSTRAINT fk_product_images_product_tenant
                FOREIGN KEY (tenant_id, product_id)
                REFERENCES products (tenant_id, id)
                ON DELETE RESTRICT;

            ALTER TABLE product_images
                ADD CONSTRAINT fk_product_images_variant_tenant
                FOREIGN KEY (tenant_id, product_variant_id)
                REFERENCES product_variants (tenant_id, id)
                ON DELETE RESTRICT;

            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS ck_sales_returns_total_requested_qty;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS ck_sales_returns_total_received_qty;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS ck_sales_returns_total_refund_amount;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS ck_sales_returns_total_exchange_amount;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS ck_sales_returns_return_status;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS ck_sales_returns_return_channel;

            ALTER TABLE sales_returns
                ADD CONSTRAINT ck_sales_returns_total_requested_qty CHECK (total_requested_qty >= 0);
            ALTER TABLE sales_returns
                ADD CONSTRAINT ck_sales_returns_total_received_qty CHECK (total_received_qty >= 0);
            ALTER TABLE sales_returns
                ADD CONSTRAINT ck_sales_returns_total_refund_amount CHECK (total_refund_amount >= 0);
            ALTER TABLE sales_returns
                ADD CONSTRAINT ck_sales_returns_total_exchange_amount CHECK (total_exchange_amount >= 0);
            ALTER TABLE sales_returns
                ADD CONSTRAINT ck_sales_returns_return_status
                CHECK (return_status IN ('REQUESTED', 'APPROVED', 'RECEIVED', 'COMPLETED', 'CANCELLED', 'REJECTED'));
            ALTER TABLE sales_returns
                ADD CONSTRAINT ck_sales_returns_return_channel
                CHECK (return_channel IN ('POS', 'ONLINE', 'PHONE', 'OTHER'));

            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS ck_sales_return_lines_quantity_requested;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS ck_sales_return_lines_quantity_received;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS ck_sales_return_lines_quantity_received_lte_requested;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS ck_sales_return_lines_unit_price_snapshot;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS ck_sales_return_lines_unit_tax_amount_snapshot;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS ck_sales_return_lines_line_subtotal_amount;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS ck_sales_return_lines_line_tax_amount;

            ALTER TABLE sales_return_lines
                ADD CONSTRAINT ck_sales_return_lines_quantity_requested CHECK (quantity_requested > 0);
            ALTER TABLE sales_return_lines
                ADD CONSTRAINT ck_sales_return_lines_quantity_received
                CHECK (quantity_received IS NULL OR quantity_received >= 0);
            ALTER TABLE sales_return_lines
                ADD CONSTRAINT ck_sales_return_lines_quantity_received_lte_requested
                CHECK (quantity_received IS NULL OR quantity_received <= quantity_requested);
            ALTER TABLE sales_return_lines
                ADD CONSTRAINT ck_sales_return_lines_unit_price_snapshot CHECK (unit_price_snapshot >= 0);
            ALTER TABLE sales_return_lines
                ADD CONSTRAINT ck_sales_return_lines_unit_tax_amount_snapshot CHECK (unit_tax_amount_snapshot >= 0);
            ALTER TABLE sales_return_lines
                ADD CONSTRAINT ck_sales_return_lines_line_subtotal_amount CHECK (line_subtotal_amount >= 0);
            ALTER TABLE sales_return_lines
                ADD CONSTRAINT ck_sales_return_lines_line_tax_amount CHECK (line_tax_amount >= 0);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS ck_sales_return_lines_quantity_requested;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS ck_sales_return_lines_quantity_received;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS ck_sales_return_lines_quantity_received_lte_requested;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS ck_sales_return_lines_unit_price_snapshot;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS ck_sales_return_lines_unit_tax_amount_snapshot;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS ck_sales_return_lines_line_subtotal_amount;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS ck_sales_return_lines_line_tax_amount;

            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS ck_sales_returns_total_requested_qty;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS ck_sales_returns_total_received_qty;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS ck_sales_returns_total_refund_amount;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS ck_sales_returns_total_exchange_amount;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS ck_sales_returns_return_status;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS ck_sales_returns_return_channel;

            ALTER TABLE product_images DROP CONSTRAINT IF EXISTS fk_product_images_product_tenant;
            ALTER TABLE product_images DROP CONSTRAINT IF EXISTS fk_product_images_variant_tenant;
            ALTER TABLE product_images
                ADD CONSTRAINT fk_product_images_product_id_products
                FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE RESTRICT;
            ALTER TABLE product_images
                ADD CONSTRAINT fk_product_images_product_variant_id_product_variants
                FOREIGN KEY (product_variant_id) REFERENCES product_variants (id) ON DELETE RESTRICT;

            DROP INDEX IF EXISTS uq_sales_return_lines_tenant_return_order_line;

            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS fk_sales_return_lines_return_tenant;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS fk_sales_return_lines_order_line_tenant;
            ALTER TABLE sales_return_lines DROP CONSTRAINT IF EXISTS fk_sales_return_lines_return_reason_tenant;
            ALTER TABLE sales_return_lines
                ADD CONSTRAINT fk_sales_return_lines_6cf63e7f
                FOREIGN KEY (sales_return_id) REFERENCES sales_returns (id) ON DELETE RESTRICT;
            ALTER TABLE sales_return_lines
                ADD CONSTRAINT fk_sales_return_lines_1e6282f1
                FOREIGN KEY (sales_order_line_id) REFERENCES sales_order_lines (id) ON DELETE RESTRICT;
            ALTER TABLE sales_return_lines
                ADD CONSTRAINT fk_sales_return_lines_4427f485
                FOREIGN KEY (return_reason_id) REFERENCES return_reasons (id) ON DELETE RESTRICT;

            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS fk_sales_returns_document_number_sequence_tenant;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS fk_sales_returns_sales_order_tenant;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS fk_sales_returns_customer_tenant;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS fk_sales_returns_outlet_tenant;
            ALTER TABLE sales_returns DROP CONSTRAINT IF EXISTS fk_sales_returns_return_reason_tenant;
            ALTER TABLE sales_returns
                ADD CONSTRAINT fk_sales_returns_1549e8c2
                FOREIGN KEY (document_number_sequence_id) REFERENCES document_number_sequences (id) ON DELETE RESTRICT;
            ALTER TABLE sales_returns
                ADD CONSTRAINT fk_sales_returns_8e3771a4
                FOREIGN KEY (sales_order_id) REFERENCES sales_orders (id) ON DELETE RESTRICT;
            ALTER TABLE sales_returns
                ADD CONSTRAINT fk_sales_returns_f8fcc58d
                FOREIGN KEY (customer_id) REFERENCES customers (id) ON DELETE RESTRICT;
            ALTER TABLE sales_returns
                ADD CONSTRAINT fk_sales_returns_8bbbda2e
                FOREIGN KEY (outlet_id) REFERENCES outlets (id) ON DELETE RESTRICT;
            ALTER TABLE sales_returns
                ADD CONSTRAINT fk_sales_returns_8308f645
                FOREIGN KEY (return_reason_id) REFERENCES return_reasons (id) ON DELETE RESTRICT;

            DROP INDEX IF EXISTS uq_product_images_tenant_id_id;
            DROP INDEX IF EXISTS uq_sales_return_lines_tenant_id_id;
            DROP INDEX IF EXISTS uq_sales_returns_tenant_id_id;
            DROP INDEX IF EXISTS uq_return_reasons_tenant_id_id;
            DROP INDEX IF EXISTS uq_outlets_tenant_id_id;
            """);
    }
}
