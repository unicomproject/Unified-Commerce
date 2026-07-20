using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Step 5 Select Reason hardening:
/// database-driven requires_note / requires_manager_approval / description,
/// and restored applies_to / sort_order CHECK constraints.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260717140000_HardenReturnReasonsConfigurableFlags")]
public partial class HardenReturnReasonsConfigurableFlags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE return_reasons
                ADD COLUMN IF NOT EXISTS description varchar(500) NULL;

            ALTER TABLE return_reasons
                ADD COLUMN IF NOT EXISTS requires_note boolean NOT NULL DEFAULT false;

            ALTER TABLE return_reasons
                ADD COLUMN IF NOT EXISTS requires_manager_approval boolean NOT NULL DEFAULT false;

            -- Preserve prior OTHER-requires-notes behavior for existing rows.
            UPDATE return_reasons
            SET requires_note = true,
                updated_at = now()
            WHERE UPPER(reason_code) = 'OTHER'
              AND requires_note = false;

            -- Safe CHECK restore: normalize invalid applies_to / sort_order first.
            UPDATE return_reasons
            SET applies_to = 'RETURN',
                updated_at = now()
            WHERE applies_to IS NULL
               OR UPPER(TRIM(applies_to)) NOT IN ('RETURN', 'EXCHANGE', 'BOTH');

            UPDATE return_reasons
            SET sort_order = 0,
                updated_at = now()
            WHERE sort_order < 0;

            ALTER TABLE return_reasons DROP CONSTRAINT IF EXISTS ck_return_reasons_applies_to;
            ALTER TABLE return_reasons DROP CONSTRAINT IF EXISTS ck_return_reasons_sort_order;

            ALTER TABLE return_reasons
                ADD CONSTRAINT ck_return_reasons_applies_to
                CHECK (applies_to IN ('RETURN', 'EXCHANGE', 'BOTH'));

            ALTER TABLE return_reasons
                ADD CONSTRAINT ck_return_reasons_sort_order
                CHECK (sort_order >= 0);

            CREATE UNIQUE INDEX IF NOT EXISTS uq_return_reasons_tenant_id_id
                ON return_reasons (tenant_id, id);

            -- Refresh development seed configurable fields (stable IDs preserved).
            INSERT INTO return_reasons (
                id, tenant_id, reason_code, reason_name, description, applies_to,
                requires_note, requires_inspection, requires_manager_approval,
                is_active, sort_order, created_at, updated_at
            )
            VALUES
                ('dddd0001-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'DAMAGED', 'Damaged Item', 'Item was damaged when received or opened.', 'RETURN', false, false, false, true, 0, now(), now()),
                ('dddd0001-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'WRONG_ITEM', 'Wrong Item', 'Customer received a different product than ordered.', 'RETURN', false, false, false, true, 1, now(), now()),
                ('dddd0001-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'SIZE_ISSUE', 'Size / Fit Issue', 'Size or fit does not meet customer expectation.', 'BOTH', false, false, false, true, 2, now(), now()),
                ('dddd0001-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'CHANGED_MIND', 'Changed Mind', 'Customer no longer wants the item.', 'RETURN', false, false, false, true, 3, now(), now()),
                ('dddd0001-0005-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'DEFECTIVE', 'Product Defective', 'Product has a manufacturing or functional defect.', 'RETURN', false, true, false, true, 4, now(), now()),
                ('dddd0001-0006-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'OTHER', 'Other', 'Provide a brief note describing the return reason.', 'BOTH', true, false, false, true, 5, now(), now())
            ON CONFLICT (tenant_id, reason_code) DO UPDATE
            SET reason_name = EXCLUDED.reason_name,
                description = EXCLUDED.description,
                applies_to = EXCLUDED.applies_to,
                requires_note = EXCLUDED.requires_note,
                requires_inspection = EXCLUDED.requires_inspection,
                requires_manager_approval = EXCLUDED.requires_manager_approval,
                is_active = true,
                sort_order = EXCLUDED.sort_order,
                updated_at = now();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE return_reasons DROP CONSTRAINT IF EXISTS ck_return_reasons_applies_to;
            ALTER TABLE return_reasons DROP CONSTRAINT IF EXISTS ck_return_reasons_sort_order;

            ALTER TABLE return_reasons DROP COLUMN IF EXISTS requires_manager_approval;
            ALTER TABLE return_reasons DROP COLUMN IF EXISTS requires_note;
            ALTER TABLE return_reasons DROP COLUMN IF EXISTS description;
            """);
    }
}
