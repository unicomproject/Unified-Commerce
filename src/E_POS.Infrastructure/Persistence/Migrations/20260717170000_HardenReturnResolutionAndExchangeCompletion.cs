using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260717170000_HardenReturnResolutionAndExchangeCompletion")]
public partial class HardenReturnResolutionAndExchangeCompletion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE return_inspection_drafts
                ADD COLUMN IF NOT EXISTS requires_inspection boolean NOT NULL DEFAULT false;
            ALTER TABLE return_inspection_drafts
                ADD COLUMN IF NOT EXISTS requires_manager_approval boolean NOT NULL DEFAULT false;
            ALTER TABLE sales_exchanges
                ADD COLUMN IF NOT EXISTS idempotency_key varchar(120) NULL;

            CREATE UNIQUE INDEX IF NOT EXISTS uq_tenant_users_tenant_id_id
                ON tenant_users (tenant_id, id);

            CREATE UNIQUE INDEX IF NOT EXISTS ux_sales_exchanges_tenant_idempotency_key
                ON sales_exchanges (tenant_id, idempotency_key)
                WHERE idempotency_key IS NOT NULL;

            CREATE UNIQUE INDEX IF NOT EXISTS ux_sales_exchanges_sales_return
                ON sales_exchanges (tenant_id, sales_return_id)
                WHERE sales_return_id IS NOT NULL;

            ALTER TABLE return_inspection_drafts
                DROP CONSTRAINT IF EXISTS ck_return_inspection_drafts_resolution_type;
            ALTER TABLE return_inspection_drafts
                ADD CONSTRAINT ck_return_inspection_drafts_resolution_type
                CHECK (resolution_type IS NULL OR resolution_type IN ('REFUND', 'EXCHANGE'));

            ALTER TABLE return_inspection_drafts
                DROP CONSTRAINT IF EXISTS ck_return_inspection_drafts_resolution_fields;
            ALTER TABLE return_inspection_drafts
                ADD CONSTRAINT ck_return_inspection_drafts_resolution_fields
                CHECK (
                    (resolution_type IS NULL
                        AND resolution_selected_at IS NULL
                        AND resolution_selected_by_tenant_user_id IS NULL)
                    OR
                    (resolution_type IS NOT NULL
                        AND resolution_selected_at IS NOT NULL
                        AND resolution_selected_by_tenant_user_id IS NOT NULL)
                );

            ALTER TABLE return_inspection_drafts
                DROP CONSTRAINT IF EXISTS fk_return_inspection_drafts_resolution_user_tenant;
            ALTER TABLE return_inspection_drafts
                ADD CONSTRAINT fk_return_inspection_drafts_resolution_user_tenant
                FOREIGN KEY (tenant_id, resolution_selected_by_tenant_user_id)
                REFERENCES tenant_users (tenant_id, id)
                ON DELETE RESTRICT;

            ALTER TABLE return_exchange_replacement_draft_lines
                DROP CONSTRAINT IF EXISTS fk_return_exchange_replacement_draft_lines_return_inspection_drafts_return_inspection_draft_id;
            ALTER TABLE return_exchange_replacement_draft_lines
                DROP CONSTRAINT IF EXISTS fk_return_exchange_replacement_draft_lines_draft_tenant;
            ALTER TABLE return_exchange_replacement_draft_lines
                ADD CONSTRAINT fk_return_exchange_replacement_draft_lines_draft_tenant
                FOREIGN KEY (tenant_id, return_inspection_draft_id)
                REFERENCES return_inspection_drafts (tenant_id, id)
                ON DELETE RESTRICT;

            ALTER TABLE return_exchange_replacement_draft_lines
                DROP CONSTRAINT IF EXISTS ck_return_exchange_replacement_draft_lines_quantity;
            ALTER TABLE return_exchange_replacement_draft_lines
                ADD CONSTRAINT ck_return_exchange_replacement_draft_lines_quantity
                CHECK (quantity > 0);

            ALTER TABLE return_exchange_replacement_draft_lines
                DROP CONSTRAINT IF EXISTS fk_return_exchange_replacement_draft_lines_user_tenant;
            ALTER TABLE return_exchange_replacement_draft_lines
                ADD CONSTRAINT fk_return_exchange_replacement_draft_lines_user_tenant
                FOREIGN KEY (tenant_id, selected_by_tenant_user_id)
                REFERENCES tenant_users (tenant_id, id)
                ON DELETE RESTRICT;

            ALTER TABLE return_exchange_replacement_draft_lines
                DROP CONSTRAINT IF EXISTS fk_return_exchange_replacement_draft_lines_sale_line_tenant;
            ALTER TABLE return_exchange_replacement_draft_lines
                ADD CONSTRAINT fk_return_exchange_replacement_draft_lines_sale_line_tenant
                FOREIGN KEY (tenant_id, returned_sale_line_id)
                REFERENCES sales_order_lines (tenant_id, id)
                ON DELETE RESTRICT;

            ALTER TABLE return_exchange_replacement_draft_lines
                DROP CONSTRAINT IF EXISTS fk_return_exchange_replacement_draft_lines_product_tenant;
            ALTER TABLE return_exchange_replacement_draft_lines
                ADD CONSTRAINT fk_return_exchange_replacement_draft_lines_product_tenant
                FOREIGN KEY (tenant_id, replacement_product_id)
                REFERENCES products (tenant_id, id)
                ON DELETE RESTRICT;

            ALTER TABLE return_exchange_replacement_draft_lines
                DROP CONSTRAINT IF EXISTS fk_return_exchange_replacement_draft_lines_variant_tenant;
            ALTER TABLE return_exchange_replacement_draft_lines
                ADD CONSTRAINT fk_return_exchange_replacement_draft_lines_variant_tenant
                FOREIGN KEY (tenant_id, replacement_variant_id)
                REFERENCES product_variants (tenant_id, id)
                ON DELETE RESTRICT;

            CREATE OR REPLACE FUNCTION protect_return_inspection_draft_resolution()
            RETURNS trigger AS $$
            BEGIN
                IF OLD.status = 'CONSUMED' THEN
                    RAISE EXCEPTION 'Consumed return inspection drafts are immutable';
                END IF;
                IF OLD.expires_at <= CURRENT_TIMESTAMP THEN
                    RAISE EXCEPTION 'Expired return inspection drafts cannot be mutated';
                END IF;
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            DROP TRIGGER IF EXISTS trg_protect_return_inspection_draft_resolution
                ON return_inspection_drafts;
            CREATE TRIGGER trg_protect_return_inspection_draft_resolution
                BEFORE UPDATE OF resolution_type, resolution_selected_at,
                    resolution_selected_by_tenant_user_id, version
                ON return_inspection_drafts
                FOR EACH ROW
                EXECUTE FUNCTION protect_return_inspection_draft_resolution();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS trg_protect_return_inspection_draft_resolution
                ON return_inspection_drafts;
            DROP FUNCTION IF EXISTS protect_return_inspection_draft_resolution();
            ALTER TABLE return_exchange_replacement_draft_lines
                DROP CONSTRAINT IF EXISTS fk_return_exchange_replacement_draft_lines_user_tenant;
            ALTER TABLE return_exchange_replacement_draft_lines
                DROP CONSTRAINT IF EXISTS fk_return_exchange_replacement_draft_lines_sale_line_tenant;
            ALTER TABLE return_exchange_replacement_draft_lines
                DROP CONSTRAINT IF EXISTS fk_return_exchange_replacement_draft_lines_product_tenant;
            ALTER TABLE return_exchange_replacement_draft_lines
                DROP CONSTRAINT IF EXISTS fk_return_exchange_replacement_draft_lines_variant_tenant;
            ALTER TABLE return_exchange_replacement_draft_lines
                DROP CONSTRAINT IF EXISTS fk_return_exchange_replacement_draft_lines_draft_tenant;
            ALTER TABLE return_exchange_replacement_draft_lines
                DROP CONSTRAINT IF EXISTS ck_return_exchange_replacement_draft_lines_quantity;
            ALTER TABLE return_inspection_drafts
                DROP CONSTRAINT IF EXISTS fk_return_inspection_drafts_resolution_user_tenant;
            ALTER TABLE return_inspection_drafts
                DROP CONSTRAINT IF EXISTS ck_return_inspection_drafts_resolution_fields;
            ALTER TABLE return_inspection_drafts
                DROP CONSTRAINT IF EXISTS ck_return_inspection_drafts_resolution_type;
            DROP INDEX IF EXISTS ux_sales_exchanges_tenant_idempotency_key;
            DROP INDEX IF EXISTS ux_sales_exchanges_sales_return;
            DROP INDEX IF EXISTS uq_tenant_users_tenant_id_id;
            ALTER TABLE sales_exchanges
                DROP COLUMN IF EXISTS idempotency_key;
            ALTER TABLE return_inspection_drafts
                DROP COLUMN IF EXISTS requires_inspection;
            ALTER TABLE return_inspection_drafts
                DROP COLUMN IF EXISTS requires_manager_approval;
            """);
    }
}
