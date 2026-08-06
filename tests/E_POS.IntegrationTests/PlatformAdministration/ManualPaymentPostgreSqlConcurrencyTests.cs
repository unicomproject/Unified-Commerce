using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class ManualPaymentPostgreSqlConcurrencyTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";
    private static readonly DateTimeOffset Now = new(2026, 8, 4, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ConcurrentReviews_AllowExactlyOneTerminalDecision()
    {
        if (!await CanConnectAsync()) return;
        var ids = FixtureIds.Create();
        await SeedAsync(ids);
        try
        {
            await using var firstDb = CreateDb();
            await using var secondDb = CreateDb();
            IManualPaymentRepository first = new ManualPaymentRepository(firstDb);
            IManualPaymentRepository second = new ManualPaymentRepository(secondDb);
            var firstCommand = new ManualPaymentReviewCommand(ids.PaymentId, ManualPaymentConstants.Approve, 2,
                "Bank statement verified.", null, new string('a', 64), new string('b', 64), Guid.NewGuid(),
                ids.ReviewerOneId, Now.AddMinutes(5));
            var secondCommand = new ManualPaymentReviewCommand(ids.PaymentId, ManualPaymentConstants.Reject, 2,
                "Reference could not be verified.", "REFERENCE_NOT_FOUND", new string('c', 64), new string('d', 64),
                Guid.NewGuid(), ids.ReviewerTwoId, Now.AddMinutes(5));

            var results = await Task.WhenAll(first.ReviewAsync(firstCommand, default), second.ReviewAsync(secondCommand, default));

            Assert.Single(results, x => x.Outcome == ManualPaymentMutationOutcome.Success);
            Assert.Single(results, x => x.Outcome == ManualPaymentMutationOutcome.ConcurrencyConflict);
            await using var assertDb = CreateDb();
            Assert.Single(await assertDb.SubscriptionPaymentReviews.Where(x => x.PaymentId == ids.PaymentId).ToListAsync());
            var payment = await assertDb.SubscriptionPaymentTransactions.AsNoTracking().SingleAsync(x => x.Id == ids.PaymentId);
            Assert.Contains(payment.TransactionStatus, new[] { ManualPaymentConstants.Paid, ManualPaymentConstants.Rejected });
        }
        finally
        {
            await CleanupAsync(ids);
        }
    }

    [Fact]
    public async Task PaidTenant_ConcurrentActivation_ActivatesOnceAndQueuesOneInvitation()
    {
        if (!await CanConnectAsync()) return;
        var ids = FixtureIds.Create();
        await SeedAsync(ids);
        try
        {
            await using (var reviewDb = CreateDb())
            {
                IManualPaymentRepository reviews = new ManualPaymentRepository(reviewDb);
                var approval = await reviews.ReviewAsync(new(ids.PaymentId, ManualPaymentConstants.Approve, 2,
                    "Bank statement verified.", null, new string('a', 64), new string('b', 64), Guid.NewGuid(),
                    ids.ReviewerOneId, Now.AddMinutes(5)), default);
                Assert.Equal(ManualPaymentMutationOutcome.Success, approval.Outcome);
            }

            await using var firstDb = CreateDb();
            await using var secondDb = CreateDb();
            var results = await Task.WhenAll(
                new PlatformTenantRepository(firstDb).ActivateTenantRuntimeAsync(ids.TenantId, ids.ReviewerOneId,
                    Now.AddMinutes(6), default),
                new PlatformTenantRepository(secondDb).ActivateTenantRuntimeAsync(ids.TenantId, ids.ReviewerTwoId,
                    Now.AddMinutes(6), default));

            Assert.Single(results, x => x.Outcome == PlatformTenantActivationRuntimeOutcome.Success);
            Assert.Single(results, x => x.Outcome == PlatformTenantActivationRuntimeOutcome.Replay);
            await using var assertDb = CreateDb();
            Assert.Equal(TenantStatusConstants.Active,
                (await assertDb.Tenants.AsNoTracking().SingleAsync(x => x.Id == ids.TenantId)).Status);
            Assert.Single(await assertDb.IntegrationOutboxMessages.AsNoTracking().Where(x =>
                x.TenantId == ids.TenantId && x.MessageType == "tenant_admin.invitation_requested").ToListAsync());
        }
        finally
        {
            await CleanupAsync(ids);
        }
    }

    [Fact]
    public async Task ManualPaymentMigration_ExposesRequiredPostgreSqlConstraints()
    {
        if (!await CanConnectAsync()) return;
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT
              to_regclass('public.subscription_payment_evidence') IS NOT NULL,
              to_regclass('public.subscription_payment_reviews') IS NOT NULL,
              to_regclass('public.uq_subscription_payment_reviews_payment_idempotency') IS NOT NULL,
              EXISTS (SELECT 1 FROM information_schema.columns
                      WHERE table_name = 'subscription_payment_transactions' AND column_name = 'version'),
              EXISTS (SELECT 1 FROM information_schema.columns
                      WHERE table_name = 'subscription_payment_links' AND column_name = 'payment_transaction_id'),
              EXISTS (SELECT 1 FROM information_schema.columns
                      WHERE table_name = 'subscription_payment_reviews' AND column_name = 'expected_amount_snapshot'),
              to_regclass('public.uq_subscription_payment_transactions_provider_event') IS NOT NULL,
              to_regclass('public.uq_subscription_payment_links_active_purpose') IS NOT NULL
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        for (var index = 0; index < 8; index++) Assert.True(reader.GetBoolean(index));
    }

    [Fact]
    public async Task Migrations_ApplyToCleanPostgreSqlDatabase()
    {
        if (!await CanConnectAsync()) return;
        var databaseName = $"flow4_manual_payment_migration_{Guid.NewGuid():N}";
        var adminConnectionString = new NpgsqlConnectionStringBuilder(ConnectionString) { Database = "postgres" }.ConnectionString;
        await using (var admin = new NpgsqlConnection(adminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var cleanConnectionString = new NpgsqlConnectionStringBuilder(ConnectionString)
            {
                Database = databaseName,
                IncludeErrorDetail = true
            }.ConnectionString;
            await using var cleanDb = new EPosDbContext(
                new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(cleanConnectionString).Options);
            foreach (var migration in cleanDb.Database.GetMigrations())
            {
                try
                {
                    await cleanDb.Database.MigrateAsync(migration);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"Clean database migration failed at {migration}.", exception);
                }
            }

            Assert.True(await cleanDb.Database.CanConnectAsync());
            Assert.False(await cleanDb.SubscriptionPaymentEvidence.AnyAsync());
            var applied = await cleanDb.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, migration => migration.EndsWith("_AddFlow4ManualPaymentRuntime", StringComparison.Ordinal));
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(adminConnectionString);
            await admin.OpenAsync();
            await using (var terminate = new NpgsqlCommand(
                             "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()", admin))
            {
                terminate.Parameters.AddWithValue("database", databaseName);
                await terminate.ExecuteNonQueryAsync();
            }

            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\"", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task SeedAsync(FixtureIds ids)
    {
        await using var db = CreateDb();
        var roleTemplate = await db.RoleTemplateVersions.AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .Select(x => new { x.RoleTemplateId, VersionId = x.Id })
            .FirstAsync();
        db.PlatformUsers.AddRange(
            PlatformUser.Create(ids.ReviewerOneId, $"reviewer-one-{ids.Suffix}@example.test", "HASH", PlatformAuthConstants.ActiveStatus, Now),
            PlatformUser.Create(ids.ReviewerTwoId, $"reviewer-two-{ids.Suffix}@example.test", "HASH", PlatformAuthConstants.ActiveStatus, Now));
        db.SubscriptionPlans.Add(SubscriptionPlan.Create(ids.PlanId, $"MP-{ids.Suffix}", "Manual Payment Plan",
            SubscriptionPlanConstants.Status.Active, SubscriptionPlanConstants.BillingInterval.Monthly, 125m, Now, "LKR"));
        db.Tenants.Add(Tenant.Create(ids.TenantId, $"MP-{ids.Suffix}", $"mp-{ids.Suffix}", "Manual Payment Tenant",
            TenantStatusConstants.PendingPayment, "LKR", "Asia/Colombo", null, null, Now));
        db.TenantSubscriptions.Add(TenantSubscription.Create(ids.SubscriptionId, ids.TenantId, ids.PlanId, "ACTIVE",
            "monthly", null, null, Now, Now.AddMonths(1), false, null, null, 0, $"billing-{ids.Suffix}@example.test",
            "manual", null, null, null, null, "LKR", 125m, Now, Now, Now.AddMonths(1), ids.ReviewerOneId, Now));
        db.SubscriptionInvoices.Add(SubscriptionInvoice.CreateDraft(ids.InvoiceId, ids.TenantId, ids.SubscriptionId,
            $"INV-MP-{ids.Suffix}", 125m, "monthly", Now.AddDays(7), "LKR", Now, Now.AddMonths(1), Now));
        var payment = SubscriptionPaymentTransaction.CreateAwaitingManual(ids.PaymentId, ids.TenantId, ids.SubscriptionId,
            ids.InvoiceId, 125m, "LKR", $"MANUAL-{ids.Suffix}", Now);
        payment.SubmitManual(125m, "LKR", "BANK_TRANSFER", $"BANK-{ids.Suffix}", Now, null,
            new string('e', 64), new string('f', 64), "PAYMENT_RECIPIENT", null, Now.AddMinutes(1));
        db.SubscriptionPaymentTransactions.Add(payment);
        db.SubscriptionPaymentEvidence.Add(SubscriptionPaymentEvidence.Create(ids.EvidenceId, ids.TenantId,
            ids.PaymentId, ids.InvoiceId, "private-test", $"manual/{ids.EvidenceId:D}.pdf", "proof.pdf", "proof.pdf",
            "application/pdf", 100, new string('1', 64), 1, ManualPaymentConstants.ScanClean, Now.AddMinutes(1)));
        db.TenantUsers.Add(TenantUser.CreatePendingInvite(ids.AdminUserId, ids.TenantId,
            $"tenant-admin-{ids.Suffix}@example.test", "Tenant Admin", null, null, Now));
        db.TenantRoles.Add(TenantRole.Create(ids.RoleId, ids.TenantId, roleTemplate.RoleTemplateId,
            roleTemplate.VersionId, "TENANT_ADMIN", "Tenant Admin",
            "Tenant administrator", false, true, null, Now));
        db.TenantUserRoles.Add(TenantUserRole.Create(ids.UserRoleId, ids.TenantId, ids.AdminUserId, ids.RoleId, null, Now));
        var draft = PlatformTenantOnboardingDraft.Create(ids.DraftId, ids.ReviewerOneId, "{}", 7, 127, 100,
            Now, Now.AddDays(30));
        draft.BeginFinalization(new string('2', 64), new string('3', 64), ids.ReviewerOneId, Now);
        draft.Complete(ids.TenantId, ids.ReviewerOneId, Now);
        db.PlatformTenantOnboardingDrafts.Add(draft);
        db.PlatformTenantOnboardingOperations.Add(PlatformTenantOnboardingOperation.CreateCompleted(ids.OperationId,
            ids.DraftId, ids.TenantId, new string('2', 64), new string('3', 64),
            ManualPaymentConstants.AwaitingPayment, "NOT_ELIGIBLE", Now));
        await db.SaveChangesAsync();
    }

    private static async Task CleanupAsync(FixtureIds ids)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            DELETE FROM integration_outbox_messages WHERE tenant_id = @tenant;
            DELETE FROM subscription_payment_reviews WHERE payment_id = @payment;
            DELETE FROM subscription_payment_evidence WHERE payment_id = @payment;
            DELETE FROM subscription_payment_links WHERE payment_transaction_id = @payment;
            DELETE FROM subscription_payment_transactions WHERE id = @payment;
            DELETE FROM subscription_invoices WHERE id = @invoice;
            DELETE FROM platform_tenant_onboarding_operations WHERE tenant_id = @tenant;
            DELETE FROM platform_tenant_onboarding_drafts WHERE id = @draft;
            DELETE FROM tenant_subscription_history WHERE tenant_id = @tenant;
            DELETE FROM tenant_subscriptions WHERE id = @subscription;
            DELETE FROM tenant_user_roles WHERE id = @userRole;
            DELETE FROM tenant_roles WHERE id = @role;
            DELETE FROM tenant_users WHERE id = @adminUser;
            DELETE FROM tenants WHERE id = @tenant;
            DELETE FROM subscription_plans WHERE id = @plan;
            DELETE FROM platform_users WHERE id IN (@reviewerOne, @reviewerTwo);
            """, connection);
        command.Parameters.AddWithValue("tenant", ids.TenantId);
        command.Parameters.AddWithValue("payment", ids.PaymentId);
        command.Parameters.AddWithValue("invoice", ids.InvoiceId);
        command.Parameters.AddWithValue("subscription", ids.SubscriptionId);
        command.Parameters.AddWithValue("plan", ids.PlanId);
        command.Parameters.AddWithValue("draft", ids.DraftId);
        command.Parameters.AddWithValue("userRole", ids.UserRoleId);
        command.Parameters.AddWithValue("role", ids.RoleId);
        command.Parameters.AddWithValue("adminUser", ids.AdminUserId);
        command.Parameters.AddWithValue("reviewerOne", ids.ReviewerOneId);
        command.Parameters.AddWithValue("reviewerTwo", ids.ReviewerTwoId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch { return false; }
    }

    private static EPosDbContext CreateDb() => new(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(ConnectionString).Options);

    private sealed record FixtureIds(Guid TenantId, Guid PlanId, Guid SubscriptionId, Guid InvoiceId,
        Guid PaymentId, Guid EvidenceId, Guid ReviewerOneId, Guid ReviewerTwoId, Guid AdminUserId,
        Guid RoleId, Guid UserRoleId, Guid DraftId, Guid OperationId, string Suffix)
    {
        public static FixtureIds Create()
        {
            var suffix = Guid.NewGuid().ToString("N")[..10];
            return new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), suffix);
        }
    }
}
