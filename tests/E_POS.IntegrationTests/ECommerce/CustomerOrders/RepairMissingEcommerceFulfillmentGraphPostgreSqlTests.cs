using E_POS.Infrastructure.Persistence.Migrations;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.ECommerce.CustomerOrders;

public sealed class RepairMissingEcommerceFulfillmentGraphPostgreSqlTests
{
    private const string BaseConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    [Fact]
    public async Task Migration_RepairsOnlyOrdersWithAConfirmedReservationAndSkipsCancelledOrOrphanedOrders()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        var databaseName = $"repair_fulfillment_graph_{Guid.NewGuid():N}";
        var adminConnectionString = new NpgsqlConnectionStringBuilder(BaseConnectionString) { Database = "postgres" }.ConnectionString;
        var connectionString = new NpgsqlConnectionStringBuilder(BaseConnectionString)
        {
            Database = databaseName,
            IncludeErrorDetail = true,
        }.ConnectionString;

        await using (var admin = new NpgsqlConnection(adminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using (var baseline = new NpgsqlCommand(
                """
                CREATE TABLE sales_orders (
                    id uuid PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    order_number text NOT NULL,
                    order_type text NOT NULL,
                    order_status text NOT NULL,
                    fulfillment_method_outlet_id uuid NULL,
                    requested_collection_at timestamptz NULL,
                    requested_collection_end_at timestamptz NULL,
                    collection_timezone_snapshot text NULL,
                    customer_name_snapshot text NULL,
                    customer_phone_snapshot text NULL,
                    customer_email_snapshot text NULL);
                CREATE TABLE sales_order_lines (
                    id uuid PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    sales_order_id uuid NOT NULL,
                    quantity numeric NOT NULL,
                    cancelled_quantity numeric NOT NULL);
                CREATE TABLE checkout_sessions (
                    id uuid PRIMARY KEY,
                    converted_order_id uuid NULL,
                    inventory_reservation_id uuid NULL);
                CREATE TABLE inventory_reservations (
                    id uuid PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    source_reference_id uuid NULL,
                    source_reference_number text NULL,
                    reservation_status text NOT NULL,
                    reservation_source text NOT NULL,
                    expires_at timestamptz NULL,
                    updated_at timestamptz NULL);
                CREATE TABLE inventory_reservation_lines (
                    id uuid PRIMARY KEY,
                    inventory_reservation_id uuid NOT NULL);
                CREATE TABLE inventory_balances (
                    id uuid PRIMARY KEY,
                    inventory_location_id uuid NOT NULL);
                CREATE TABLE inventory_reservation_allocations (
                    id uuid PRIMARY KEY,
                    inventory_reservation_line_id uuid NOT NULL,
                    inventory_balance_id uuid NOT NULL);
                CREATE TABLE fulfillment_orders (
                    id uuid PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    sales_order_id uuid NOT NULL,
                    fulfillment_number text NOT NULL,
                    fulfillment_method_outlet_id uuid NOT NULL,
                    source_inventory_location_id uuid NULL,
                    fulfillment_status text NOT NULL,
                    requested_fulfillment_date date NULL,
                    scheduled_at timestamptz NULL,
                    row_version bigint NOT NULL,
                    created_at timestamptz NOT NULL,
                    updated_at timestamptz NOT NULL);
                CREATE TABLE fulfillment_order_lines (
                    id uuid PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    fulfillment_order_id uuid NOT NULL,
                    sales_order_line_id uuid NOT NULL,
                    requested_quantity numeric NOT NULL,
                    picked_quantity numeric NOT NULL,
                    packed_quantity numeric NOT NULL,
                    fulfilled_quantity numeric NOT NULL,
                    cancelled_quantity numeric NOT NULL,
                    line_status text NOT NULL,
                    created_at timestamptz NOT NULL,
                    updated_at timestamptz NOT NULL);
                CREATE TABLE pickup_slots (
                    id uuid PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    fulfillment_method_outlet_id uuid NOT NULL,
                    slot_code text NOT NULL,
                    slot_date date NOT NULL,
                    window_start time NOT NULL,
                    window_end time NOT NULL,
                    capacity int NOT NULL,
                    reserved_count int NOT NULL,
                    slot_status text NOT NULL,
                    row_version bigint NOT NULL,
                    created_at timestamptz NOT NULL,
                    updated_at timestamptz NOT NULL);
                CREATE TABLE pickup_slot_reservations (
                    id uuid PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    pickup_slot_id uuid NOT NULL,
                    sales_order_id uuid NOT NULL,
                    reserved_capacity int NOT NULL,
                    reservation_status text NOT NULL,
                    expires_at timestamptz NULL,
                    confirmed_at timestamptz NULL,
                    created_at timestamptz NOT NULL,
                    updated_at timestamptz NOT NULL);
                CREATE TABLE pickup_orders (
                    id uuid PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    fulfillment_order_id uuid NOT NULL,
                    pickup_slot_reservation_id uuid NULL,
                    pickup_number text NOT NULL,
                    pickup_contact_name text NOT NULL,
                    pickup_contact_phone text NULL,
                    pickup_contact_email text NULL,
                    pickup_contact_channel text NULL,
                    pickup_status text NOT NULL,
                    created_at timestamptz NOT NULL,
                    updated_at timestamptz NOT NULL);
                """,
                connection))
            {
                await baseline.ExecuteNonQueryAsync();
            }

            var tenantId = Guid.NewGuid();
            var outletId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            // Order 1: repairable — CLICK_AND_COLLECT, CONFIRMED, has a checkout session
            // with a CONFIRMED reservation whose stock was allocated from a single location.
            var repairableOrderId = Guid.NewGuid();
            var repairableLineId = Guid.NewGuid();
            var checkoutId = Guid.NewGuid();
            var reservationId = Guid.NewGuid();
            var reservationLineId = Guid.NewGuid();
            var balanceId = Guid.NewGuid();

            // Order 2: cancelled — must never be repaired even though it otherwise
            // qualifies, since a cancelled order has no operational fulfilment to do.
            var cancelledOrderId = Guid.NewGuid();
            var cancelledCheckoutId = Guid.NewGuid();
            var cancelledReservationId = Guid.NewGuid();

            // Order 3: orphaned — a directly-seeded CLICK_AND_COLLECT order with no
            // checkout session at all. Repairing it would mean fabricating a reservation
            // that never existed, so it must be left untouched (detected as missing, not
            // silently patched).
            var orphanedOrderId = Guid.NewGuid();

            await using (var seed = new NpgsqlCommand(
                """
                INSERT INTO sales_orders (
                    id, tenant_id, order_number, order_type, order_status,
                    fulfillment_method_outlet_id, requested_collection_at,
                    requested_collection_end_at, collection_timezone_snapshot,
                    customer_name_snapshot, customer_phone_snapshot, customer_email_snapshot)
                VALUES
                    (@repairableOrder, @tenant, 'ORD-000001', 'CLICK_AND_COLLECT', 'CONFIRMED',
                     @outlet, '2026-09-15T11:00:00+05:30', '2026-09-15T11:30:00+05:30', 'Asia/Colombo',
                     'Jane Doe', '+94770000000', 'jane@example.com'),
                    (@cancelledOrder, @tenant, 'ORD-000002', 'CLICK_AND_COLLECT', 'CANCELLED',
                     @outlet, '2026-09-15T11:00:00+05:30', '2026-09-15T11:30:00+05:30', 'Asia/Colombo',
                     'John Roe', '+94770000001', 'john@example.com'),
                    (@orphanedOrder, @tenant, 'ECOMM-001', 'CLICK_AND_COLLECT', 'CONFIRMED',
                     @outlet, '2026-09-15T11:00:00+05:30', '2026-09-15T11:30:00+05:30', 'Asia/Colombo',
                     'No Checkout', NULL, NULL);

                INSERT INTO sales_order_lines (id, tenant_id, sales_order_id, quantity, cancelled_quantity)
                VALUES (@repairableLine, @tenant, @repairableOrder, 2, 0);

                INSERT INTO checkout_sessions (id, converted_order_id, inventory_reservation_id)
                VALUES
                    (@checkout, @repairableOrder, @reservation),
                    (@cancelledCheckout, @cancelledOrder, @cancelledReservation);

                INSERT INTO inventory_reservations (
                    id, tenant_id, source_reference_id, source_reference_number,
                    reservation_status, reservation_source, expires_at, updated_at)
                VALUES
                    (@reservation, @tenant, @checkout, 'CHK-1', 'CONFIRMED', 'CHECKOUT',
                     now() + interval '10 minutes', now()),
                    (@cancelledReservation, @tenant, @cancelledCheckout, 'CHK-2', 'CONFIRMED', 'CHECKOUT',
                     NULL, now());

                INSERT INTO inventory_reservation_lines (id, inventory_reservation_id)
                VALUES (@reservationLine, @reservation);

                INSERT INTO inventory_balances (id, inventory_location_id)
                VALUES (@balance, @location);

                INSERT INTO inventory_reservation_allocations (
                    id, inventory_reservation_line_id, inventory_balance_id)
                VALUES (gen_random_uuid(), @reservationLine, @balance);
                """,
                connection))
            {
                seed.Parameters.AddWithValue("tenant", tenantId);
                seed.Parameters.AddWithValue("outlet", outletId);
                seed.Parameters.AddWithValue("location", locationId);
                seed.Parameters.AddWithValue("repairableOrder", repairableOrderId);
                seed.Parameters.AddWithValue("repairableLine", repairableLineId);
                seed.Parameters.AddWithValue("checkout", checkoutId);
                seed.Parameters.AddWithValue("reservation", reservationId);
                seed.Parameters.AddWithValue("reservationLine", reservationLineId);
                seed.Parameters.AddWithValue("balance", balanceId);
                seed.Parameters.AddWithValue("cancelledOrder", cancelledOrderId);
                seed.Parameters.AddWithValue("cancelledCheckout", cancelledCheckoutId);
                seed.Parameters.AddWithValue("cancelledReservation", cancelledReservationId);
                seed.Parameters.AddWithValue("orphanedOrder", orphanedOrderId);
                await seed.ExecuteNonQueryAsync();
            }

            // Apply the migration's actual shipped SQL twice: the second application
            // must be a true no-op, proving the repair is idempotent and cannot ever
            // create a duplicate fulfilment graph.
            for (var attempt = 0; attempt < 2; attempt++)
            {
                await using (var repair = new NpgsqlCommand(
                    RepairMissingEcommerceFulfillmentGraph.RepairFulfillmentGraphSql, connection))
                    await repair.ExecuteNonQueryAsync();
                await using (var repoint = new NpgsqlCommand(
                    RepairMissingEcommerceFulfillmentGraph.RepointReservationSourceReferenceSql, connection))
                    await repoint.ExecuteNonQueryAsync();
                await using (var clearExpiry = new NpgsqlCommand(
                    RepairMissingEcommerceFulfillmentGraph.ClearStaleReservationExpirySql, connection))
                    await clearExpiry.ExecuteNonQueryAsync();
            }

            await using var assertCommand = new NpgsqlCommand(
                """
                SELECT
                  (SELECT count(*) FROM fulfillment_orders WHERE sales_order_id = @repairableOrder) AS repaired_count,
                  (SELECT count(*) FROM fulfillment_order_lines fol
                     JOIN fulfillment_orders fo ON fo.id = fol.fulfillment_order_id
                     WHERE fo.sales_order_id = @repairableOrder) AS repaired_line_count,
                  (SELECT count(*) FROM pickup_orders po
                     JOIN fulfillment_orders fo ON fo.id = po.fulfillment_order_id
                     WHERE fo.sales_order_id = @repairableOrder) AS repaired_pickup_count,
                  (SELECT count(*) FROM fulfillment_orders WHERE sales_order_id = @cancelledOrder) AS cancelled_count,
                  (SELECT count(*) FROM fulfillment_orders WHERE sales_order_id = @orphanedOrder) AS orphaned_count,
                  (SELECT source_reference_id FROM inventory_reservations WHERE id = @reservation) AS repointed_source,
                  (SELECT expires_at FROM inventory_reservations WHERE id = @reservation) AS repointed_expiry,
                  (SELECT row_version FROM fulfillment_orders WHERE sales_order_id = @repairableOrder) AS row_version
                """,
                connection);
            assertCommand.Parameters.AddWithValue("repairableOrder", repairableOrderId);
            assertCommand.Parameters.AddWithValue("cancelledOrder", cancelledOrderId);
            assertCommand.Parameters.AddWithValue("orphanedOrder", orphanedOrderId);
            assertCommand.Parameters.AddWithValue("reservation", reservationId);
            await using var reader = await assertCommand.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());

            Assert.Equal(1L, reader.GetInt64(0));
            Assert.Equal(1L, reader.GetInt64(1));
            Assert.Equal(1L, reader.GetInt64(2));
            Assert.Equal(0L, reader.GetInt64(3));
            Assert.Equal(0L, reader.GetInt64(4));
            Assert.Equal(repairableOrderId, reader.GetGuid(5));
            Assert.True(reader.IsDBNull(6));
            Assert.Equal(1L, reader.GetInt64(7));
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(adminConnectionString);
            await admin.OpenAsync();
            await using (var terminate = new NpgsqlCommand(
                             "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()",
                             admin))
            {
                terminate.Parameters.AddWithValue("database", databaseName);
                await terminate.ExecuteNonQueryAsync();
            }

            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\"", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(BaseConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
