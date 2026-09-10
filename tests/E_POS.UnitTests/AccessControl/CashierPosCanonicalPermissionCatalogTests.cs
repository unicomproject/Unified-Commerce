using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

/// <summary>
/// Chunk 2 integrity tests for the Cashier POS canonical permission definition catalog.
/// Does not assert runtime authorization, seeding, or UI gating.
/// </summary>
public sealed class CashierPosCanonicalPermissionCatalogTests
{
    [Fact]
    public void AllCodes_AreUnique()
    {
        var codes = CashierPosCanonicalPermissionCatalog.All.Select(d => d.Code).ToList();
        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void AllRoleAssignableCodes_MatchFourTierTaxonomy()
    {
        foreach (var definition in CashierPosCanonicalPermissionCatalog.RoleAssignable)
        {
            var parts = definition.Code.Split('.');
            Assert.True(
                parts.Length == 4,
                $"Expected 4-tier code, got '{definition.Code}' ({parts.Length} segments).");
            Assert.Equal(definition.Code, definition.Code.ToLowerInvariant());
            Assert.All(parts, part => Assert.False(string.IsNullOrWhiteSpace(part)));
        }
    }

    [Fact]
    public void PreAuthCodes_AreExcludedFromRoleAssignable()
    {
        var preAuth = CashierPosCanonicalPermissionCatalog.All
            .Where(d => d.Kind == CashierPosPermissionDefinitionKind.PreAuth)
            .ToList();

        Assert.Equal(7, preAuth.Count);
        Assert.All(preAuth, d => Assert.False(d.IsRoleAssignable));
        Assert.All(
            preAuth,
            d => Assert.Equal(
                CashierPosPermissionSemanticType.PreAuthConfiguration,
                d.SemanticType));
        Assert.DoesNotContain(
            CashierPosCanonicalPermissionCatalog.RoleAssignable,
            d => d.Kind == CashierPosPermissionDefinitionKind.PreAuth);
    }

    [Fact]
    public void Parents_Exist_And_Graph_Has_No_Cycles()
    {
        var byCode = CashierPosCanonicalPermissionCatalog.All.ToDictionary(
            d => d.Code,
            StringComparer.Ordinal);

        foreach (var definition in CashierPosCanonicalPermissionCatalog.All)
        {
            if (definition.ParentCode is null)
            {
                continue;
            }

            Assert.True(
                byCode.ContainsKey(definition.ParentCode),
                $"Unknown parent '{definition.ParentCode}' for '{definition.Code}'.");
        }

        foreach (var start in CashierPosCanonicalPermissionCatalog.All.Where(d => d.ParentCode is not null))
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var current = start.Code;
            while (byCode.TryGetValue(current, out var node) && node.ParentCode is not null)
            {
                Assert.True(seen.Add(current), $"Cycle detected at '{current}'.");
                current = node.ParentCode;
            }
        }
    }

    [Fact]
    public void SensitiveFlags_Only_On_RoleAssignable_Definitions()
    {
        foreach (var definition in CashierPosCanonicalPermissionCatalog.All.Where(d => d.IsSensitive))
        {
            Assert.True(definition.IsRoleAssignable);
            Assert.NotEqual(
                CashierPosPermissionDefinitionKind.PreAuth,
                definition.Kind);
        }
    }

    [Fact]
    public void Chunk1ClassificationCounts_MatchFinalizedKinds()
    {
        var all = CashierPosCanonicalPermissionCatalog.All;
        Assert.Equal(14, all.Count(d => d.Kind == CashierPosPermissionDefinitionKind.New));
        Assert.Equal(280, all.Count(d => d.Kind == CashierPosPermissionDefinitionKind.Split));
        Assert.Equal(
            1,
            all.Count(d => d.Kind == CashierPosPermissionDefinitionKind.DocumentedResolved));
        Assert.Equal(7, all.Count(d => d.Kind == CashierPosPermissionDefinitionKind.PreAuth));
        Assert.Contains(
            all,
            d => d.Code == "pos.sales.held_sales.cancel"
                && d.Kind == CashierPosPermissionDefinitionKind.DocumentedResolved);
    }

    [Fact]
    public void ExistingCanonicalPaymentMethods_RemainIndependent()
    {
        Assert.Equal("pos.payments.cash.accept", PaymentPermissions.AcceptCash);
        Assert.Equal("pos.payments.card.accept", PaymentPermissions.AcceptCard);
        Assert.Equal("pos.payments.qr.accept", PaymentPermissions.AcceptQr);
        Assert.Equal("pos.payments.split.accept", PaymentPermissions.AcceptSplit);

        var codes = CashierPosCanonicalPermissionCatalog.All
            .Select(d => d.Code)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains(PaymentPermissions.AcceptCash, codes);
        Assert.Contains(PaymentPermissions.AcceptCard, codes);
        Assert.Contains(PaymentPermissions.AcceptQr, codes);
        Assert.Contains(PaymentPermissions.AcceptSplit, codes);
    }

    [Fact]
    public void ExistingCanonicalParents_RemainUnchanged()
    {
        string[] locked =
        [
            "pos.sales.dashboard.view",
            "pos.sales.new_sale.view",
            "pos.sales.new_sale.create",
            "pos.sales.catalog.view",
            "pos.sales.catalog.search",
            "pos.sales.checkout.execute",
            "pos.sales.held_sales.create",
            "pos.sales.held_sales.view",
            "pos.sales.held_sales.recall",
            "pos.payments.cash.accept",
            "pos.receipts.digital.view",
            "pos.customers.management.view",
            "pos.notifications.alerts.view",
            "pos.cash_drawer.position.view",
            "pos.cash_drawer.physical.manage",
            "pos.cash_drawer.movements.create",
            "pos.till.session.open",
            "pos.till.session.close",
            "pos.till.session.view",
        ];

        var existing = CashierPosCanonicalPermissionCatalog.All
            .Where(d => d.Kind == CashierPosPermissionDefinitionKind.Existing)
            .Select(d => d.Code)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var code in locked)
        {
            Assert.Contains(code, existing);
        }
    }

    [Fact]
    public void NoDuplicateCanonicalCapabilityMappings_InFineGrainedSet()
    {
        var fine = CashierPosCanonicalPermissionCatalog.FineGrained
            .Select(d => d.Code)
            .ToList();
        Assert.Equal(fine.Count, fine.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(295, fine.Count); // 14 new + 280 split + 1 documented_resolved
    }

    [Fact]
    public void QuickAmountSlots_DoNotEncodeCurrencyValues()
    {
        var quickAmountCodes = CashierPosCanonicalPermissionCatalog.All
            .Select(d => d.Code)
            .Where(c => c.Contains("quick", StringComparison.Ordinal));

        Assert.All(
            quickAmountCodes,
            code =>
            {
                Assert.DoesNotContain("100", code, StringComparison.Ordinal);
                Assert.DoesNotContain("500", code, StringComparison.Ordinal);
                Assert.DoesNotContain("1000", code, StringComparison.Ordinal);
            });
    }

    [Fact]
    public void CatalogCounts_MatchTotalAndRoleAssignableCounts()
    {
        var all = CashierPosCanonicalPermissionCatalog.All;
        Assert.Equal(353, all.Count);
        Assert.Equal(353, all.Select(d => d.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(346, CashierPosCanonicalPermissionCatalog.RoleAssignable.Count());
        Assert.Equal(295, CashierPosCanonicalPermissionCatalog.FineGrained.Count());
    }

    [Fact]
    public void EveryDefinition_HasValidProperties()
    {
        foreach (var definition in CashierPosCanonicalPermissionCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(definition.Code));
            Assert.False(string.IsNullOrWhiteSpace(definition.Module));
            Assert.False(string.IsNullOrWhiteSpace(definition.Reason));
            if (definition.ParentCode is not null)
            {
                Assert.False(string.IsNullOrWhiteSpace(definition.ParentCode));
            }
        }
    }
}
