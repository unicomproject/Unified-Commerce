using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using E_POS.Infrastructure.Persistence.Migrations;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

/// <summary>
/// Chunk 3 seed / format / compatibility migration integrity tests.
/// Does not exercise live PostgreSQL; validates generated SQL and catalog contracts.
/// </summary>
public sealed class CashierPosChunk3PermissionSeedTests
{
    [Fact]
    public void RoleAssignableCodes_MatchChunk2Counts_AndExcludePreAuth()
    {
        Assert.Equal(346, CashierPosChunk3PermissionSeedData.RoleAssignableDefinitions.Count);
        Assert.Equal(295, CashierPosChunk3PermissionSeedData.FineGrainedCount);
        Assert.Equal(51, CashierPosChunk3PermissionSeedData.ExistingCatalogCount);
        Assert.Equal(7, CashierPosChunk3PermissionSeedData.PreAuthExcludedCount);

        Assert.DoesNotContain(
            CashierPosChunk3PermissionSeedData.RoleAssignableDefinitions,
            d => d.Kind == CashierPosPermissionDefinitionKind.PreAuth
                || d.Code.StartsWith("pre_auth.", StringComparison.Ordinal));
    }

    [Fact]
    public void EveryRoleAssignableCode_IsExactlyFourTier_WithoutWildcardsOrSlashes()
    {
        CashierPosPermissionCodeTaxonomy.EnsureAllRoleAssignableCodesAreValid();

        foreach (var definition in CashierPosChunk3PermissionSeedData.RoleAssignableDefinitions)
        {
            Assert.True(
                CashierPosPermissionCodeTaxonomy.IsValidFourTierCanonicalCode(definition.Code),
                definition.Code);
            Assert.DoesNotContain("*", definition.Code, StringComparison.Ordinal);
            Assert.DoesNotContain("/", definition.Code, StringComparison.Ordinal);
            Assert.DoesNotContain(" ", definition.Code, StringComparison.Ordinal);
            Assert.Equal(definition.Code, definition.Code.ToLowerInvariant());
        }
    }

    [Theory]
    [InlineData("pos.till.session.open/close")]
    [InlineData("pos.cash_drawer.*")]
    [InlineData("payments.cash.accept")]
    [InlineData("pos.payments.cash")]
    [InlineData("pos.payments.cash.accept.extra")]
    [InlineData("pos..cash.accept")]
    [InlineData(".pos.payments.cash.accept")]
    [InlineData("POS.payments.cash.accept")]
    public void InvalidFormats_AreRejected(string code)
    {
        Assert.False(CashierPosPermissionCodeTaxonomy.IsValidFourTierCanonicalCode(code));
        Assert.Throws<InvalidOperationException>(
            () => CashierPosPermissionCodeTaxonomy.EnsureValidFourTierCanonicalCode(code));
    }

    [Theory]
    [InlineData("pos.till.session.open")]
    [InlineData("pos.till.session.close")]
    [InlineData("pos.payments.cash.accept")]
    [InlineData("pos.cash_drawer.movements.cash_in")]
    [InlineData("pos.sales.held_sales.cancel")]
    public void ValidFormats_AreAccepted(string code)
    {
        Assert.True(CashierPosPermissionCodeTaxonomy.IsValidFourTierCanonicalCode(code));
    }

    [Fact]
    public void SeedSql_ContainsEveryRoleAssignableCode_ExactlyOnce_AndNoPreAuth()
    {
        var sql = CashierPosChunk3PermissionSeedData.DefinitionUpsertSql;

        foreach (var definition in CashierPosChunk3PermissionSeedData.RoleAssignableDefinitions)
        {
            var token = $"'{definition.Code}'";
            var occurrences = CountOccurrences(sql, token);
            Assert.True(occurrences >= 1, $"Missing seed row for {definition.Code}");
            Assert.True(occurrences <= 2, $"Duplicated seed token for {definition.Code}: {occurrences}");
        }

        Assert.DoesNotContain("pre_auth.login.", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("'*'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("open/close", sql, StringComparison.Ordinal);
        Assert.Contains("ON CONFLICT (permission_code) DO UPDATE", sql, StringComparison.Ordinal);
        Assert.Contains("cashier-pos-chunk3-permission:", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void CompatibilityPairs_HaveValidParents_AndCoverCashFamily()
    {
        var pairs = CashierPosChunk3PermissionSeedData.CompatibilityPairs;
        Assert.NotEmpty(pairs);

        var byCode = CashierPosCanonicalPermissionCatalog.All
            .ToDictionary(d => d.Code, StringComparer.Ordinal);

        foreach (var (parent, child) in pairs)
        {
            Assert.True(byCode.ContainsKey(parent), parent);
            Assert.True(byCode.ContainsKey(child), child);
            Assert.True(CashierPosPermissionCodeTaxonomy.IsValidFourTierCanonicalCode(parent));
            Assert.True(CashierPosPermissionCodeTaxonomy.IsValidFourTierCanonicalCode(child));
        }

        var cashChildren = pairs
            .Where(p => p.ParentCode == PaymentPermissions.AcceptCash)
            .Select(p => p.ChildCode)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("pos.cash_payment.tender.exact", cashChildren);
        Assert.Contains("pos.cash_payment.numpad.container", cashChildren);
        Assert.Contains("pos.cash_payment.completion.execute", cashChildren);
        Assert.Equal(35, cashChildren.Count);
    }

    [Fact]
    public void CompatibilityBackfillSql_IsTenantScoped_Idempotent_AndNotesMarked()
    {
        var sql = CashierPosChunk3PermissionSeedData.CompatibilityBackfillSql;

        Assert.Contains("tenant_role_permissions", sql, StringComparison.Ordinal);
        Assert.Contains("tenant_user_permissions", sql, StringComparison.Ordinal);
        Assert.Contains("trp.tenant_id", sql, StringComparison.Ordinal);
        Assert.Contains("tup.tenant_id", sql, StringComparison.Ordinal);
        Assert.Contains("revoked_at IS NULL", sql, StringComparison.Ordinal);
        Assert.Contains("ON CONFLICT DO NOTHING", sql, StringComparison.Ordinal);
        Assert.Contains("ON CONFLICT (tenant_id, user_id, permission_id) DO NOTHING", sql, StringComparison.Ordinal);
        Assert.Contains(CashierPosChunk3PermissionSeedData.CompatibilityRoleNotes, sql, StringComparison.Ordinal);
        Assert.Contains("pos.payments.cash.accept", sql, StringComparison.Ordinal);
        Assert.Contains("pos.cash_payment.tender.exact", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void SpecialBusinessMigrations_AreParentLinked()
    {
        var pairs = CashierPosChunk3PermissionSeedData.CompatibilityPairs
            .ToLookup(p => p.ChildCode, p => p.ParentCode);

        Assert.Contains("pos.sales.held_sales.create", pairs["pos.sales.held_sales.cancel"]);
        Assert.Contains("pos.cash_drawer.movements.create", pairs["pos.cash_drawer.movements.cash_in"]);
        Assert.Contains("pos.cash_drawer.movements.create", pairs["pos.cash_drawer.movements.cash_out"]);
        Assert.Contains("pos.cash_drawer.movements.create", pairs["pos.cash_drawer.movements.cash_drop"]);
        Assert.Contains("pos.customers.management.view", pairs["pos.customers.management.attach_sale"]);
        Assert.Contains("pos.customers.management.update", pairs["pos.customers.management.deactivate"]);
        Assert.Contains("pos.sales.dashboard.view", pairs["pos.shell.navigation.settings"]);
        Assert.Contains("pos.returns.search_sale.view", pairs["pos.home.actions.returns_entry"]);
        Assert.Contains("commerce.online_order.orders.access", pairs["pos.home.actions.online_orders_entry"]);
    }

    [Fact]
    public void DownSql_RemovesOnlyFineGrainedChunk3Rows_NotExistingBusinessCodes()
    {
        var down = CashierPosChunk3PermissionSeedData.DownSql;

        Assert.Contains("pos.cash_payment.tender.exact", down, StringComparison.Ordinal);
        Assert.Contains("pos.sales.held_sales.cancel", down, StringComparison.Ordinal);
        Assert.Contains(CashierPosChunk3PermissionSeedData.CompatibilityRoleNotes, down, StringComparison.Ordinal);
        Assert.Contains("cashier-pos-chunk3-permission:", down, StringComparison.Ordinal);

        // Existing business parents must not be deleted by FineGrained down list alone.
        Assert.DoesNotContain("'pos.payments.cash.accept'", down, StringComparison.Ordinal);
        Assert.DoesNotContain("'pos.sales.dashboard.view'", down, StringComparison.Ordinal);
        Assert.DoesNotContain("'pos.notifications.alerts.view'", down, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_AppliesSeedSql_WithoutSchemaChanges()
    {
        var migration = new SeedCashierPosChunk3CanonicalPermissions();
        var upSql = string.Join(
            '\n',
            migration.UpOperations.OfType<SqlOperation>().Select(o => o.Sql));
        var downSql = string.Join(
            '\n',
            migration.DownOperations.OfType<SqlOperation>().Select(o => o.Sql));

        Assert.Contains("INSERT INTO permission_definitions", upSql, StringComparison.Ordinal);
        Assert.Contains("tenant_role_permissions", upSql, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE TABLE", upSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ALTER TABLE", upSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DELETE FROM tenant_role_permissions", downSql, StringComparison.Ordinal);
        Assert.Contains("DELETE FROM permission_definitions", downSql, StringComparison.Ordinal);
    }

    [Fact]
    public void PaymentMethodParents_RemainIndependentlyRepresented()
    {
        var codes = CashierPosChunk3PermissionSeedData.RoleAssignableDefinitions
            .Select(d => d.Code)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(PaymentPermissions.AcceptCash, codes);
        Assert.Contains(PaymentPermissions.AcceptCard, codes);
        Assert.Contains(PaymentPermissions.AcceptQr, codes);
        Assert.Contains(PaymentPermissions.AcceptSplit, codes);
    }

    [Fact]
    public void CompatibilityPairs_AreUnique()
    {
        var pairs = CashierPosChunk3PermissionSeedData.CompatibilityPairs;
        var distinct = pairs
            .Select(p => $"{p.ParentCode}->{p.ChildCode}")
            .Distinct(StringComparer.Ordinal)
            .Count();
        Assert.Equal(pairs.Count, distinct);
    }

    [Fact]
    public void FeatureKeyResolver_NeverReturnsWildcardOrSlash()
    {
        foreach (var definition in CashierPosChunk3PermissionSeedData.RoleAssignableDefinitions)
        {
            var key = CashierPosChunk3PermissionSeedData.ResolveFeatureKey(definition.Code);
            Assert.False(string.IsNullOrWhiteSpace(key));
            Assert.DoesNotContain("*", key, StringComparison.Ordinal);
            Assert.DoesNotContain("/", key, StringComparison.Ordinal);
        }
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
