using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.Flow4FixtureCli.Tests;

public sealed class Flow4FixturePostgreSqlTests
{
    [Fact]
    public async Task All_scenarios_have_expected_state_hash_only_persistence_and_owned_cleanup()
    {
        var options = IntegrationOptions(); if (options is null) return;
        var run = Guid.NewGuid(); var store = CreateStore(options, out var db);
        await using (db)
        {
            var manifest = await store.CreateAsync(run, Enum.GetValues<Flow4FixtureScenario>());
            Assert.Equal(17, manifest.Cleanup.Scenarios.Count);
            Assert.Equal(2, manifest.Secrets.Keys.Count(x => x.EndsWith(".invitationToken", StringComparison.Ordinal)));
            foreach (var scenario in Enum.GetValues<Flow4FixtureScenario>())
                Assert.True(manifest.Identifiers.ContainsKey($"{scenario}.paymentId"));

            var payments = await db.SubscriptionPaymentTransactions.AsNoTracking().ToDictionaryAsync(x => x.Id);
            AssertStatus(manifest, payments, Flow4FixtureScenario.AWAITING_PAYMENT, ManualPaymentConstants.AwaitingPayment);
            AssertStatus(manifest, payments, Flow4FixtureScenario.PAYMENT_SUBMITTED, ManualPaymentConstants.PaymentSubmitted);
            AssertStatus(manifest, payments, Flow4FixtureScenario.ACTION_REQUIRED, ManualPaymentConstants.ActionRequired);
            AssertStatus(manifest, payments, Flow4FixtureScenario.REJECTED, ManualPaymentConstants.Rejected);
            AssertStatus(manifest, payments, Flow4FixtureScenario.CONCURRENT_REVIEW, ManualPaymentConstants.UnderReview);
            AssertStatus(manifest, payments, Flow4FixtureScenario.PAID_PENDING_ACTIVATION, ManualPaymentConstants.Paid);
            AssertStatus(manifest, payments, Flow4FixtureScenario.COMPLETE_HAPPY_PATH, ManualPaymentConstants.Paid);

            var raw = manifest.Secrets.Values.ToHashSet(StringComparer.Ordinal);
            Assert.All(await db.SubscriptionPaymentLinks.AsNoTracking().ToListAsync(), link =>
            {
                Assert.DoesNotContain(link.TokenHash ?? string.Empty, raw);
                Assert.DoesNotContain(link.PaymentLinkTokenHash ?? string.Empty, raw);
            });
            Assert.All(await db.UserInvites.AsNoTracking().ToListAsync(), invite => Assert.DoesNotContain(invite.InviteTokenHash, raw));
            var expired = Guid.Parse(manifest.Identifiers["EXPIRED_PAYMENT_ACCESS.accessId"]);
            var revoked = Guid.Parse(manifest.Identifiers["REVOKED_PAYMENT_ACCESS.accessId"]);
            Assert.False((await db.SubscriptionPaymentLinks.AsNoTracking().SingleAsync(x => x.Id == expired)).Allows("STATUS", DateTimeOffset.UtcNow));
            Assert.Equal(ManualPaymentConstants.AccessRevoked,
                (await db.SubscriptionPaymentLinks.AsNoTracking().SingleAsync(x => x.Id == revoked)).LinkStatus);

            var result = await store.CleanupAsync(run, manifest.Cleanup.Handle);
            Assert.False(result.AlreadyClean);
            Assert.True((await store.CleanupAsync(run, manifest.Cleanup.Handle)).AlreadyClean);
            var tenantIds = manifest.Identifiers.Where(x => x.Key.EndsWith("tenantId", StringComparison.OrdinalIgnoreCase))
                .Select(x => Guid.Parse(x.Value)).ToArray();
            Assert.False(await db.Tenants.AsNoTracking().AnyAsync(x => tenantIds.Contains(x.Id)));
        }
    }

    [Fact]
    public async Task Parallel_runs_are_isolated_and_foreign_handle_cannot_cleanup()
    {
        var options = IntegrationOptions(); if (options is null) return;
        var runA = Guid.NewGuid(); var runB = Guid.NewGuid();
        var storeA = CreateStore(options, out var dbA); var storeB = CreateStore(options, out var dbB);
        await using (dbA) await using (dbB)
        {
            var creates = await Task.WhenAll(
                storeA.CreateAsync(runA, [Flow4FixtureScenario.AWAITING_PAYMENT]),
                storeB.CreateAsync(runB, [Flow4FixtureScenario.AWAITING_PAYMENT]));
            Assert.NotEqual(creates[0].Identifiers["AWAITING_PAYMENT.tenantId"], creates[1].Identifiers["AWAITING_PAYMENT.tenantId"]);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => storeB.CleanupAsync(runB, creates[0].Cleanup.Handle));
            await Assert.ThrowsAsync<InvalidOperationException>(() => storeA.CreateAsync(runA, [Flow4FixtureScenario.AWAITING_PAYMENT]));
            await storeA.CleanupAsync(runA, creates[0].Cleanup.Handle);
            await storeB.CleanupAsync(runB, creates[1].Cleanup.Handle);
        }
    }

    private static void AssertStatus(Flow4FixtureManifest manifest,
        IReadOnlyDictionary<Guid, E_POS.Domain.Modules.Platform.Subscription.Entities.SubscriptionPaymentTransaction> payments,
        Flow4FixtureScenario scenario, string expected) =>
        Assert.Equal(expected, payments[Guid.Parse(manifest.Identifiers[$"{scenario}.paymentId"])].TransactionStatus);

    private static Flow4FixtureStore CreateStore(Flow4FixtureOptions options, out EPosDbContext db)
    {
        db = new(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(options.ConnectionString).Options);
        ITokenHashService hash = new TokenHashService();
        var jwt = Microsoft.Extensions.Options.Options.Create(new TenantJwtOptions { SigningKey = options.TenantSigningKey });
        return new(db, new ManualPaymentAccessTokenService(hash, jwt), new InvitationTokenService(hash, jwt),
            new PasswordHashService(), options);
    }

    private static Flow4FixtureOptions? IntegrationOptions()
    {
        var connection = Environment.GetEnvironmentVariable("FLOW4_FIXTURE_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection)) return null;
        return new("E2E", true, connection,
            Environment.GetEnvironmentVariable("FLOW4_FIXTURE_TEST_BOOTSTRAP")!,
            Environment.GetEnvironmentVariable("FLOW4_FIXTURE_TEST_MARKER")!, "SUPPRESSED",
            Environment.GetEnvironmentVariable("FLOW4_FIXTURE_TEST_SIGNING_KEY")!, 30,
            new HashSet<string>(["localhost", "127.0.0.1"]),
            new HashSet<string>(["production", "prod", "staging", "shared"]),
            new HashSet<string>(["UnifiedCommerceDb", "postgres", "production"]), "flow4_runner");
    }
}
