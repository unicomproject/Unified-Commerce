using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedFulfillmentOrderConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Some development databases contain an untracked legacy row_version
            // with default 0. Reconcile it in place without dropping the column or data.
            migrationBuilder.Sql("""
                ALTER TABLE fulfillment_orders
                    ADD COLUMN IF NOT EXISTS row_version bigint;

                UPDATE fulfillment_orders
                SET row_version = 1
                WHERE row_version IS NULL OR row_version < 1;

                ALTER TABLE fulfillment_orders
                    ALTER COLUMN row_version SET DEFAULT 1,
                    ALTER COLUMN row_version SET NOT NULL;

                DO $migration$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conrelid = 'fulfillment_orders'::regclass
                          AND conname = 'ck_fulfillment_orders_row_version'
                    ) THEN
                        ALTER TABLE fulfillment_orders
                            ADD CONSTRAINT ck_fulfillment_orders_row_version
                            CHECK (row_version >= 1);
                    END IF;
                END
                $migration$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_orders_row_version",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "fulfillment_orders");
        }
    }
}
