using System.Security.Cryptography;
using System.Text;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace E_POS.Flow4FixtureCli;

public sealed class Flow4FixtureStore(
    EPosDbContext db,
    IManualPaymentAccessTokenService paymentTokens,
    IInvitationTokenService invitationTokens,
    IPasswordHashService passwords,
    Flow4FixtureOptions options)
{
    private static readonly DateTimeOffset FixtureEpoch = new(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);

    public async Task<Flow4FixtureManifest> CreateAsync(Guid runId, IReadOnlyCollection<Flow4FixtureScenario> requested,
        CancellationToken cancellationToken = default)
    {
        var scenarios = requested.Distinct().OrderBy(x => x).ToArray();
        if (scenarios.Length == 0) throw new InvalidOperationException("At least one approved scenario is required.");
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(options.TokenTtlMinutes);
        var cleanupHandle = NewSecret();
        var cleanupHash = HashCleanup(runId, cleanupHandle);
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var secrets = new Dictionary<string, string>(StringComparer.Ordinal);
        var resources = new List<(Flow4FixtureScenario Scenario, string Type, Guid Id)>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO flow4_test_control.fixture_runs
                    (run_id, cleanup_handle_hash, fixture_set_version, status, created_at, expires_at)
                VALUES ({runId}, {cleanupHash}, {Flow4FixtureManifest.CurrentFixtureSetVersion}, 'ACTIVE', {now}, {expiresAt})
                ON CONFLICT (run_id) DO NOTHING
                """, cancellationToken);
            if (inserted != 1) throw new InvalidOperationException("This test run already exists; duplicate creation is rejected.");

            await CreatePersonasAsync(runId, now, ids, secrets, resources, cancellationToken);
            foreach (var scenario in scenarios)
            {
                var item = await CreateScenarioAsync(runId, scenario, now, expiresAt, cancellationToken);
                AddScenarioManifest(item, ids, secrets);
                resources.AddRange(ResourceEntries(item));
            }

            await db.SaveChangesAsync(cancellationToken);
            if (scenarios.Contains(Flow4FixtureScenario.EXPIRED_PAYMENT_ACCESS))
            {
                var access = Guid.Parse(ids["EXPIRED_PAYMENT_ACCESS.accessId"]);
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE subscription_payment_links SET created_at = {now.AddMinutes(-2)}, expires_at = {now.AddMinutes(-1)} WHERE id = {access}", cancellationToken);
            }

            foreach (var resource in resources)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO flow4_test_control.fixture_resources(run_id, scenario, resource_type, resource_id, created_at)
                    VALUES ({runId}, {resource.Scenario.ToString()}, {resource.Type}, {resource.Id}, {now})
                    """, cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);

            var counts = resources.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);
            var manifest = new Flow4FixtureManifest(
                new(Flow4FixtureManifest.CurrentSchemaVersion, Flow4FixtureManifest.CurrentFixtureSetVersion, runId,
                    options.Environment, now, expiresAt, null), ids, secrets,
                new(cleanupHandle, "1", scenarios.Select(x => x.ToString()).ToArray(), counts));
            manifest.Validate(runId, options.Environment);
            return manifest;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<Flow4FixtureCleanupResult> CleanupAsync(Guid runId, string cleanupHandle,
        CancellationToken cancellationToken = default)
    {
        var expected = HashCleanup(runId, cleanupHandle);
        await using var connection = new NpgsqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var owner = new NpgsqlCommand("""
            SELECT cleanup_handle_hash, status FROM flow4_test_control.fixture_runs
            WHERE run_id = @run FOR UPDATE
            """, connection, transaction);
        owner.Parameters.AddWithValue("run", runId);
        await using var reader = await owner.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            await reader.CloseAsync();
            await transaction.RollbackAsync(cancellationToken);
            return new(runId, true, new Dictionary<string, int>());
        }
        var stored = reader.GetString(0);
        var status = reader.GetString(1);
        await reader.CloseAsync();
        if (!Flow4FixtureSecurityGuard.FixedEquals(stored, expected))
            throw new UnauthorizedAccessException("Cleanup ownership validation failed.");
        if (status == "CLEANED")
        {
            await transaction.CommitAsync(cancellationToken);
            return new(runId, true, new Dictionary<string, int>());
        }

        var removed = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (name, sql) in CleanupStatements)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("run", runId);
            removed[name] = await command.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var mark = new NpgsqlCommand(
                         "UPDATE flow4_test_control.fixture_runs SET status='CLEANED', cleaned_at=now() WHERE run_id=@run",
                         connection, transaction))
        {
            mark.Parameters.AddWithValue("run", runId);
            await mark.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var ledger = new NpgsqlCommand(
                         "DELETE FROM flow4_test_control.fixture_resources WHERE run_id=@run", connection, transaction))
        {
            ledger.Parameters.AddWithValue("run", runId);
            removed["ledger"] = await ledger.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return new(runId, false, removed);
    }

    private async Task CreatePersonasAsync(Guid runId, DateTimeOffset now, IDictionary<string, string> ids,
        IDictionary<string, string> secrets, ICollection<(Flow4FixtureScenario, string, Guid)> resources,
        CancellationToken cancellationToken)
    {
        var roleCodes = new[] { "super_administrator", "billing_viewer_dev", "platform_ops_no_billing_dev" };
        var roles = await db.PlatformRoles.Where(x => roleCodes.Contains(x.RoleCode)).ToDictionaryAsync(x => x.RoleCode, cancellationToken);
        if (roles.Count != roleCodes.Length) throw new InvalidOperationException("Approved Flow 4 platform persona roles are not seeded.");
        var personas = new[]
        {
            ("ADMIN", "super_administrator"), ("SECOND_ADMIN", "super_administrator"),
            ("VIEW_ONLY", "billing_viewer_dev"), ("NO_BILLING", "platform_ops_no_billing_dev")
        };
        foreach (var (name, roleCode) in personas)
        {
            var userId = DeterministicId(runId, $"persona:{name}:user");
            var userRoleId = DeterministicId(runId, $"persona:{name}:role");
            var email = $"flow4-{name.ToLowerInvariant().Replace('_', '-')}-{runId:N}@example.test";
            var password = NewSecret();
            db.PlatformUsers.Add(PlatformUser.Create(userId, email, passwords.HashPassword(password), PlatformAuthConstants.ActiveStatus, now));
            db.PlatformUserRoles.Add(PlatformUserRole.Create(userRoleId, userId, roles[roleCode].Id, "Flow 4 isolated E2E persona", now));
            ids[$"PERSONA_{name}_EMAIL"] = email;
            secrets[$"PERSONA_{name}_PASSWORD"] = password;
            resources.Add((Flow4FixtureScenario.AWAITING_PAYMENT, "platform_user_role", userRoleId));
            resources.Add((Flow4FixtureScenario.AWAITING_PAYMENT, "platform_user", userId));
        }
    }

    private async Task<Flow4ScenarioResources> CreateScenarioAsync(Guid runId, Flow4FixtureScenario scenario,
        DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        Guid Id(string type) => DeterministicId(runId, $"{scenario}:{type}");
        var tenantId = Id("tenant"); var planId = Id("plan"); var subscriptionId = Id("subscription");
        var invoiceId = Id("invoice"); var paymentId = Id("payment"); var draftId = Id("draft");
        var operationId = Id("operation"); var accessId = Id("access"); var adminId = Id("admin");
        var roleId = Id("tenant-role"); var userRoleId = Id("tenant-user-role");
        var suffix = $"{runId:N}"[..8] + "-" + scenario.ToString().ToLowerInvariant()[..Math.Min(12, scenario.ToString().Length)];
        var roleTemplate = await db.RoleTemplateVersions.AsNoTracking().OrderBy(x => x.CreatedAt)
            .Select(x => new { x.RoleTemplateId, VersionId = x.Id }).FirstAsync(cancellationToken);
        var reviewerId = DeterministicId(runId, "persona:ADMIN:user");

        db.SubscriptionPlans.Add(SubscriptionPlan.Create(planId, $"F4-{suffix}", "Flow 4 Fixture Plan",
            SubscriptionPlanConstants.Status.Active, SubscriptionPlanConstants.BillingInterval.Monthly, 125m, FixtureEpoch, "LKR"));
        var tenant = Tenant.Create(tenantId, $"F4-{suffix}", $"f4-{suffix}", "Flow 4 Fixture Tenant",
            TenantStatusConstants.PendingPayment, "LKR", "Asia/Colombo", null, null, now);
        db.Tenants.Add(tenant);
        db.TenantSubscriptions.Add(TenantSubscription.Create(subscriptionId, tenantId, planId, "ACTIVE", "monthly",
            null, null, now, now.AddMonths(1), false, null, null, 0, $"billing-{suffix}@example.test", "manual",
            null, null, null, null, "LKR", 125m, now, now, now.AddMonths(1), reviewerId, now));
        db.SubscriptionInvoices.Add(SubscriptionInvoice.CreateDraft(invoiceId, tenantId, subscriptionId,
            $"INV-F4-{suffix}", 125m, "monthly", now.AddDays(7), "LKR", now, now.AddMonths(1), now));
        var payment = SubscriptionPaymentTransaction.CreateAwaitingManual(paymentId, tenantId, subscriptionId,
            invoiceId, 125m, "LKR", $"MANUAL-{suffix}", now);
        db.SubscriptionPaymentTransactions.Add(payment);
        db.TenantUsers.Add(TenantUser.CreatePendingInvite(adminId, tenantId, $"tenant-admin-{suffix}@example.test",
            "Tenant Admin", null, null, now));
        db.TenantRoles.Add(TenantRole.Create(roleId, tenantId, roleTemplate.RoleTemplateId, roleTemplate.VersionId,
            "TENANT_ADMIN", "Tenant Admin", "Tenant administrator", false, true, null, now));
        db.TenantUserRoles.Add(TenantUserRole.Create(userRoleId, tenantId, adminId, roleId, null, now));
        var draft = PlatformTenantOnboardingDraft.Create(draftId, reviewerId, "{}", 7, 127, 100, now, now.AddDays(1));
        draft.BeginFinalization(new string('2', 64), new string('3', 64), reviewerId, now);
        draft.Complete(tenantId, reviewerId, now);
        db.PlatformTenantOnboardingDrafts.Add(draft);
        var operation = PlatformTenantOnboardingOperation.CreateCompleted(operationId, draftId, tenantId,
            new string('2', 64), new string('3', 64), ManualPaymentConstants.AwaitingPayment, "NOT_ELIGIBLE", now);
        db.PlatformTenantOnboardingOperations.Add(operation);

        string? rawPayment = null; string? rawInvite = null; Guid? evidenceId = null; Guid? inviteId = null;
        var needsSubmitted = scenario is not (Flow4FixtureScenario.AWAITING_PAYMENT or Flow4FixtureScenario.EXPIRED_PAYMENT_ACCESS
            or Flow4FixtureScenario.REVOKED_PAYMENT_ACCESS or Flow4FixtureScenario.RETRYABLE_OPERATION);
        if (needsSubmitted)
        {
            payment.SubmitManual(125m, "LKR", "BANK_TRANSFER", $"BANK-{suffix}", now, null,
                new string('e', 64), new string('f', 64), "PAYMENT_RECIPIENT", null, now);
            operation.MarkPaymentSubmitted(now);
            if (scenario != Flow4FixtureScenario.CROSS_TENANT_PROOF)
            {
                evidenceId = Id("evidence");
                var scan = scenario == Flow4FixtureScenario.UNCLEAN_EVIDENCE ? "INFECTED" : ManualPaymentConstants.ScanClean;
                db.SubscriptionPaymentEvidence.Add(SubscriptionPaymentEvidence.Create(evidenceId.Value, tenantId, paymentId,
                    invoiceId, "flow4-test-metadata-only", $"flow4/{runId:D}/{evidenceId:D}.pdf", "proof.pdf", "proof.pdf",
                    "application/pdf", 100, new string('1', 64), 1, scan, now));
            }
        }
        if (scenario is Flow4FixtureScenario.ACTION_REQUIRED or Flow4FixtureScenario.REQUEST_INFORMATION_ELIGIBLE)
        { payment.BeginReview(now); payment.RequestInformation(reviewerId, "MORE_INFO", "Controlled fixture request", now); operation.MarkPaymentReviewOutcome(ManualPaymentConstants.ActionRequired, now); }
        else if (scenario is Flow4FixtureScenario.REJECTED)
        { payment.BeginReview(now); payment.Reject(reviewerId, "NO_MATCH", "Controlled fixture rejection", now); operation.MarkPaymentReviewOutcome(ManualPaymentConstants.Rejected, now); }
        else if (scenario is Flow4FixtureScenario.PAID_PENDING_ACTIVATION or Flow4FixtureScenario.ACTIVE_INVITATION_READY or Flow4FixtureScenario.COMPLETE_HAPPY_PATH)
        { payment.BeginReview(now); payment.Approve(reviewerId, 125m, "Controlled fixture approval", now); operation.MarkPaymentReviewOutcome(ManualPaymentConstants.Paid, now); }
        else if (scenario == Flow4FixtureScenario.CONCURRENT_REVIEW) payment.BeginReview(now);
        if (scenario == Flow4FixtureScenario.RETRYABLE_OPERATION)
            operation.MarkRetryable("CONTROLLED_TEST_FAILURE", "Controlled fixture failure", now.AddMinutes(1), now);

        var recipient = $"billing-{suffix}@example.test";
        rawPayment = paymentTokens.GenerateToken();
        var access = SubscriptionPaymentLink.CreateManualAccess(accessId, tenantId, invoiceId, paymentId,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(recipient))).ToLowerInvariant(), expiresAt, now, reviewerId);
        access.ProvisionToken(paymentTokens.HashToken(rawPayment), recipient, now);
        if (scenario == Flow4FixtureScenario.REVOKED_PAYMENT_ACCESS) access.Revoke(now);
        db.SubscriptionPaymentLinks.Add(access);

        if (scenario is Flow4FixtureScenario.ACTIVE_INVITATION_READY or Flow4FixtureScenario.COMPLETE_HAPPY_PATH)
        {
            tenant.MarkPendingActivation(reviewerId, now);
            tenant.Activate(reviewerId, now); operation.MarkActivationCompleted(now);
            rawInvite = invitationTokens.GenerateToken(); inviteId = Id("invite");
            var invite = UserInvite.CreatePending(inviteId.Value, tenantId, $"tenant-admin-{suffix}@example.test",
                $"TENANT-ADMIN-{suffix.ToUpperInvariant()}@EXAMPLE.TEST", roleId, reviewerId,
                invitationTokens.HashToken(rawInvite), expiresAt, now);
            invite.MarkSent(now); db.UserInvites.Add(invite); operation.MarkInvitationSent(now);
        }
        if (scenario == Flow4FixtureScenario.NOTIFICATION_FAILED)
        {
            var outbox = IntegrationOutboxMessage.Create(Id("outbox"), "MANUAL_PAYMENT_NOTIFICATION", "PAYMENT",
                paymentId, 1, tenantId, runId, null, "{}", $"flow4:{runId:D}:{paymentId:D}", now);
            outbox.TryAcquire("flow4-fixture", now, TimeSpan.FromMinutes(1));
            outbox.MarkFailed("CONTROLLED_TEST_FAILURE", "Controlled test sink failure", false, now.AddMinutes(1), now);
            db.IntegrationOutboxMessages.Add(outbox);
        }
        Guid? secondaryTenantId = null; Guid? secondarySubscriptionId = null;
        Guid? secondaryInvoiceId = null; Guid? secondaryPaymentId = null;
        if (scenario == Flow4FixtureScenario.CROSS_TENANT_PROOF)
        {
            secondaryTenantId = Id("secondary-tenant");
            secondarySubscriptionId = Id("secondary-subscription");
            secondaryInvoiceId = Id("secondary-invoice");
            secondaryPaymentId = Id("secondary-payment");
            evidenceId = Id("secondary-evidence");
            db.Tenants.Add(Tenant.Create(secondaryTenantId.Value, $"F4-X-{suffix}", $"f4-x-{suffix}",
                "Flow 4 Cross Tenant", TenantStatusConstants.PendingPayment, "LKR", "Asia/Colombo", null, null, now));
            db.TenantSubscriptions.Add(TenantSubscription.Create(secondarySubscriptionId.Value, secondaryTenantId.Value,
                planId, "ACTIVE", "monthly", null, null, now, now.AddMonths(1), false, null, null, 0,
                $"cross-{suffix}@example.test", "manual", null, null, null, null, "LKR", 125m, now, now,
                now.AddMonths(1), reviewerId, now));
            db.SubscriptionInvoices.Add(SubscriptionInvoice.CreateDraft(secondaryInvoiceId.Value, secondaryTenantId.Value,
                secondarySubscriptionId.Value, $"INV-F4-X-{suffix}", 125m, "monthly", now.AddDays(7), "LKR", now,
                now.AddMonths(1), now));
            var crossPayment = SubscriptionPaymentTransaction.CreateAwaitingManual(secondaryPaymentId.Value,
                secondaryTenantId.Value, secondarySubscriptionId.Value, secondaryInvoiceId.Value, 125m, "LKR",
                $"MANUAL-X-{suffix}", now);
            crossPayment.SubmitManual(125m, "LKR", "BANK_TRANSFER", $"BANK-X-{suffix}", now, null,
                new string('a', 64), new string('b', 64), "PAYMENT_RECIPIENT", null, now);
            db.SubscriptionPaymentTransactions.Add(crossPayment);
            db.SubscriptionPaymentEvidence.Add(SubscriptionPaymentEvidence.Create(evidenceId.Value,
                secondaryTenantId.Value, secondaryPaymentId.Value, secondaryInvoiceId.Value, "flow4-test-metadata-only",
                $"flow4/{runId:D}/{evidenceId:D}.pdf", "proof.pdf", "proof.pdf", "application/pdf", 100,
                new string('1', 64), 1, ManualPaymentConstants.ScanClean, now));
        }
        return new(scenario, tenantId, planId, subscriptionId, invoiceId, paymentId, draftId, operationId, accessId,
            adminId, roleId, userRoleId, evidenceId, inviteId, secondaryTenantId, secondarySubscriptionId,
            secondaryInvoiceId, secondaryPaymentId, rawPayment, rawInvite, expiresAt);
    }

    private static void AddScenarioManifest(Flow4ScenarioResources item, IDictionary<string, string> ids,
        IDictionary<string, string> secrets)
    {
        var key = item.Scenario.ToString();
        ids[$"{key}.tenantId"] = item.TenantId.ToString("D"); ids[$"{key}.paymentId"] = item.PaymentId.ToString("D");
        ids[$"{key}.operationId"] = item.OperationId.ToString("D"); ids[$"{key}.accessId"] = item.AccessId.ToString("D");
        if (item.EvidenceId is { } evidence) ids[$"{key}.evidenceId"] = evidence.ToString("D");
        if (item.InvitationId is { } invite) ids[$"{key}.invitationId"] = invite.ToString("D");
        if (item.SecondaryTenantId is { } secondary) ids[$"{key}.secondaryTenantId"] = secondary.ToString("D");
        if (item.SecondaryPaymentId is { } secondaryPayment) ids[$"{key}.secondaryPaymentId"] = secondaryPayment.ToString("D");
        if (item.RawPaymentToken is { } payment) secrets[$"{key}.paymentToken"] = payment;
        if (item.RawInvitationToken is { } invitation) secrets[$"{key}.invitationToken"] = invitation;
    }

    private static IEnumerable<(Flow4FixtureScenario, string, Guid)> ResourceEntries(Flow4ScenarioResources x)
    {
        yield return (x.Scenario, "tenant", x.TenantId); yield return (x.Scenario, "plan", x.PlanId);
        yield return (x.Scenario, "subscription", x.SubscriptionId); yield return (x.Scenario, "invoice", x.InvoiceId);
        yield return (x.Scenario, "payment", x.PaymentId); yield return (x.Scenario, "draft", x.DraftId);
        yield return (x.Scenario, "operation", x.OperationId); yield return (x.Scenario, "access", x.AccessId);
        yield return (x.Scenario, "tenant_user", x.AdminUserId); yield return (x.Scenario, "tenant_role", x.RoleId);
        yield return (x.Scenario, "tenant_user_role", x.UserRoleId);
        if (x.EvidenceId is { } evidence) yield return (x.Scenario, "evidence", evidence);
        if (x.InvitationId is { } invitation) yield return (x.Scenario, "invitation", invitation);
        if (x.SecondaryTenantId is { } secondary) yield return (x.Scenario, "tenant", secondary);
        if (x.SecondarySubscriptionId is { } secondarySubscription) yield return (x.Scenario, "subscription", secondarySubscription);
        if (x.SecondaryInvoiceId is { } secondaryInvoice) yield return (x.Scenario, "invoice", secondaryInvoice);
        if (x.SecondaryPaymentId is { } secondaryPayment) yield return (x.Scenario, "payment", secondaryPayment);
    }

    private static Guid DeterministicId(Guid runId, string purpose)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"flow4-fixture-v1:{runId:D}:{purpose}"));
        Array.Resize(ref bytes, 16); bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40); bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }
    private static string NewSecret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string HashCleanup(Guid runId, string value) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes($"flow4-cleanup-v1:{runId:D}:{value}"))).ToLowerInvariant();

    private static readonly (string, string)[] CleanupStatements =
    [
        ("outbox", "DELETE FROM integration_outbox_messages WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='outbox') OR tenant_id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='tenant')"),
        ("invitation", "DELETE FROM user_invites WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='invitation')"),
        ("review", "DELETE FROM subscription_payment_reviews WHERE payment_id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='payment')"),
        ("evidence", "DELETE FROM subscription_payment_evidence WHERE payment_id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='payment')"),
        ("access", "UPDATE subscription_payment_links SET link_status='REVOKED', revoked_at=now(), token_hash=NULL, payment_link_token_hash=NULL WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='access'); DELETE FROM subscription_payment_links WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='access')"),
        ("payment", "DELETE FROM subscription_payment_transactions WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='payment')"),
        ("invoice", "DELETE FROM subscription_invoices WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='invoice')"),
        ("operation", "DELETE FROM platform_tenant_onboarding_operations WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='operation')"),
        ("draft", "DELETE FROM platform_tenant_onboarding_drafts WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='draft')"),
        ("history", "DELETE FROM tenant_subscription_history WHERE tenant_id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='tenant')"),
        ("subscription", "DELETE FROM tenant_subscriptions WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='subscription')"),
        ("tenant_user_role", "DELETE FROM tenant_user_roles WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='tenant_user_role')"),
        ("tenant_role", "DELETE FROM tenant_roles WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='tenant_role')"),
        ("tenant_user", "DELETE FROM tenant_users WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='tenant_user')"),
        ("tenant", "DELETE FROM tenants WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='tenant')"),
        ("plan", "DELETE FROM subscription_plans WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='plan')"),
        ("platform_login_audit", "DELETE FROM platform_login_audits WHERE platform_user_id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='platform_user') OR platform_auth_session_id IN (SELECT id FROM platform_auth_sessions WHERE platform_user_id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='platform_user'))"),
        ("platform_password_reset", "DELETE FROM platform_password_reset_tokens WHERE platform_user_id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='platform_user')"),
        ("platform_refresh", "DELETE FROM platform_refresh_tokens WHERE platform_user_id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='platform_user')"),
        ("platform_session", "DELETE FROM platform_auth_sessions WHERE platform_user_id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='platform_user')"),
        ("platform_user_role", "DELETE FROM platform_user_roles WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='platform_user_role')"),
        ("platform_user", "DELETE FROM platform_users WHERE id IN (SELECT resource_id FROM flow4_test_control.fixture_resources WHERE run_id=@run AND resource_type='platform_user')")
    ];
}
