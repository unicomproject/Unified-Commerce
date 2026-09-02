using E_POS.Infrastructure.Persistence.Migrations;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class DevelopmentClickCollectOrderStatusSeedDataTests
{
    [Fact]
    public void HistoricalUpSql_RemainsLimitedToOriginallyShippedResponsibility()
    {
        var sql = DevelopmentClickCollectOrderStatusSeedData.UpSql;
        Assert.Contains("ECOMM-SEED-ACCEPTED-001", sql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO sales_orders", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT INTO fulfillment_orders", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT INTO pickup_slots", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT INTO inventory_reservations", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void RepairSql_ReconcilesEveryRequiredGraphSegment()
    {
        var sql = RepairDevelopmentClickCollectFulfillmentSeedPrerequisites.RepairSql;
        Assert.Contains("INSERT INTO fulfillment_orders", sql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO fulfillment_order_lines", sql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO pickup_slots", sql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO pickup_slot_reservations", sql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO pickup_orders", sql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO inventory_reservations", sql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO inventory_reservation_lines", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void RepairSql_PreservesExistingOperationalState()
    {
        var sql = RepairDevelopmentClickCollectFulfillmentSeedPrerequisites.RepairSql;
        Assert.Contains("'PENDING'", sql, StringComparison.Ordinal);
        Assert.Contains("'CONFIRMED'", sql, StringComparison.Ordinal);
        Assert.Contains("'RESERVED'", sql, StringComparison.Ordinal);
        Assert.Contains("row_version, created_at, updated_at", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE fulfillment_orders", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE fulfillment_order_lines", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RepairSql_IsIdempotentAndExactIdentityGuarded()
    {
        var sql = RepairDevelopmentClickCollectFulfillmentSeedPrerequisites.RepairSql;
        Assert.True(sql.Split("NOT EXISTS (", StringSplitOptions.None).Length >= 8);
        Assert.Contains("55555555-0000-4000-8000-000000000001", sql, StringComparison.Ordinal);
        Assert.Contains("e0000101-0003-4000-8000-000000000001", sql, StringComparison.Ordinal);
        Assert.Contains("bbbbbbbb-0001-4000-8000-000000000001", sql, StringComparison.Ordinal);
        Assert.Contains("e0000104-0003-4000-8000-000000000001", sql, StringComparison.Ordinal);
        Assert.Contains("e0000109-0003-4000-8000-000000000001", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("gen_random_uuid", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RepairSql_DetectsConflictsAndIsSchemaNeutralAndAdditive()
    {
        var sql = RepairDevelopmentClickCollectFulfillmentSeedPrerequisites.RepairSql;
        Assert.Contains("BLOCKED — EXISTING DEVELOPMENT SEED GRAPH CONFLICT", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DELETE FROM", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP ", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRUNCATE ", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ALTER TABLE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("__EFMigrationsHistory", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdditionalAcceptedOrdersSql_ReconcilesAllEquivalentAcceptedFixtures()
    {
        var sql = RepairDevelopmentClickCollectFulfillmentSeedPrerequisites
            .AdditionalAcceptedOrdersSql;

        Assert.Contains("ECOMM-SEED-ACCEPTED-002", sql, StringComparison.Ordinal);
        Assert.Contains("ECOMM-SEED-ACCEPTED-003", sql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO fulfillment_orders", sql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO fulfillment_order_lines", sql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO pickup_slot_reservations", sql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO pickup_orders", sql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO inventory_reservations", sql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO inventory_reservation_lines", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE fulfillment_orders", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ALTER TABLE", sql, StringComparison.OrdinalIgnoreCase);
    }
}
