using System.Text.Json;
using E_POS.Application.Common.Email;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Integrations.Email;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Options;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;
using E_POS.Infrastructure.Modules.Shared.Integration.Services;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class TenantOnboardingOutboxWorkerIntegrationTests
{
    private const string ConnectionString = "Host=127.0.0.1;Port=55436;Database=oneverz_flow4_e2e_evidence;Username=postgres;Password=postgres";
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow.AddMinutes(-5);

    [Fact]
    public async Task PaymentRequired_OutboxMessage_DispatchesEmailAndUpdatesStatus()
    {
        if (!await CanConnectDbAsync()) return;

        var ids = OutboxFixtureIds.Create();
        var fakeSender = new TestRecordingEmailSender(true);
        await SeedOutboxFixtureAsync(ids, "manual_payment.access_notification_requested");

        try
        {
            var worker = CreateWorker(fakeSender);
            await worker.RunSingleBatchAsync();

            Assert.Single(fakeSender.SentMessages);
            var sent = fakeSender.SentMessages[0];
            Assert.Equal(ids.RecipientEmail, sent.ToAddress, ignoreCase: true);
            Assert.Contains($"Payment required for invoice {ids.InvoiceNumber}", sent.Subject);
            Assert.Contains("http://localhost:4200/api/v1/tenant-onboarding/payment-access/", sent.HtmlBody);

            await using var db = CreateDb();
            var outboxMsg = await db.IntegrationOutboxMessages.SingleAsync(x => x.Id == ids.OutboxMessageId);
            Assert.Equal("DELIVERED", outboxMsg.Status);
            Assert.NotNull(outboxMsg.ProcessedAt);

            // Raw token must NOT be in DB
            var link = await db.SubscriptionPaymentLinks.SingleAsync(x => x.PaymentTransactionId == ids.PaymentId);
            Assert.NotNull(link.TokenHash);
        }
        finally
        {
            await CleanupOutboxFixtureAsync(ids);
        }
    }

    [Fact]
    public async Task PaymentSubmitted_OutboxMessage_DispatchesSubmissionReceivedEmail()
    {
        if (!await CanConnectDbAsync()) return;

        var ids = OutboxFixtureIds.Create();
        var fakeSender = new TestRecordingEmailSender(true);
        await SeedOutboxFixtureAsync(ids, "manual_payment.submitted_notification_requested");

        try
        {
            var worker = CreateWorker(fakeSender);
            await worker.RunSingleBatchAsync();

            Assert.Single(fakeSender.SentMessages);
            var sent = fakeSender.SentMessages[0];
            Assert.Equal(ids.RecipientEmail, sent.ToAddress, ignoreCase: true);
            Assert.Contains($"Payment submission received for {ids.InvoiceNumber}", sent.Subject);
        }
        finally
        {
            await CleanupOutboxFixtureAsync(ids);
        }
    }

    [Fact]
    public async Task RequestInformation_OutboxMessage_DispatchesActionRequiredEmail()
    {
        if (!await CanConnectDbAsync()) return;

        var ids = OutboxFixtureIds.Create();
        var fakeSender = new TestRecordingEmailSender(true);
        await SeedOutboxFixtureAsync(ids, "manual_payment.action_required_notification_requested");

        try
        {
            var worker = CreateWorker(fakeSender);
            await worker.RunSingleBatchAsync();

            Assert.Single(fakeSender.SentMessages);
            var sent = fakeSender.SentMessages[0];
            Assert.Contains($"Payment information required for {ids.InvoiceNumber}", sent.Subject);
        }
        finally
        {
            await CleanupOutboxFixtureAsync(ids);
        }
    }

    [Fact]
    public async Task PaymentRejected_OutboxMessage_DispatchesRejectionEmail()
    {
        if (!await CanConnectDbAsync()) return;

        var ids = OutboxFixtureIds.Create();
        var fakeSender = new TestRecordingEmailSender(true);
        await SeedOutboxFixtureAsync(ids, "manual_payment.rejected_notification_requested");

        try
        {
            var worker = CreateWorker(fakeSender);
            await worker.RunSingleBatchAsync();

            Assert.Single(fakeSender.SentMessages);
            var sent = fakeSender.SentMessages[0];
            Assert.Contains($"Payment review update for {ids.InvoiceNumber}", sent.Subject);
        }
        finally
        {
            await CleanupOutboxFixtureAsync(ids);
        }
    }

    [Fact]
    public async Task PaymentApproved_OutboxMessage_DispatchesApprovalEmail()
    {
        if (!await CanConnectDbAsync()) return;

        var ids = OutboxFixtureIds.Create();
        var fakeSender = new TestRecordingEmailSender(true);
        await SeedOutboxFixtureAsync(ids, "manual_payment.approved_notification_requested");

        try
        {
            var worker = CreateWorker(fakeSender);
            await worker.RunSingleBatchAsync();

            Assert.Single(fakeSender.SentMessages);
            var sent = fakeSender.SentMessages[0];
            Assert.Contains($"Payment approved for {ids.InvoiceNumber}", sent.Subject);
        }
        finally
        {
            await CleanupOutboxFixtureAsync(ids);
        }
    }

    [Fact]
    public async Task TenantAdminInvitation_OutboxMessage_DispatchesInvitationWhenTenantIsActive()
    {
        if (!await CanConnectDbAsync()) return;

        var ids = OutboxFixtureIds.Create();
        var fakeSender = new TestRecordingEmailSender(true);
        await SeedOutboxFixtureAsync(ids, "tenant_admin.invitation_requested", setTenantActive: true);

        try
        {
            var worker = CreateWorker(fakeSender);
            await worker.RunSingleBatchAsync();

            Assert.Single(fakeSender.SentMessages);
            var sent = fakeSender.SentMessages[0];
            Assert.Equal(ids.RecipientEmail, sent.ToAddress, ignoreCase: true);
            Assert.Contains("Set up your Tenant Admin account", sent.Subject);
            Assert.Contains("http://localhost:4200/tenant-admin/setup/", sent.HtmlBody);
            Assert.DoesNotContain("/setup-account?token=", sent.HtmlBody);

            // User invites table contains hashed token
            await using var db = CreateDb();
            var invite = await db.UserInvites.SingleAsync(x => x.TenantId == ids.TenantId);
            Assert.Equal("SENT", invite.InviteStatus);
            Assert.NotNull(invite.InviteTokenHash);
        }
        finally
        {
            await CleanupOutboxFixtureAsync(ids);
        }
    }

    [Fact]
    public async Task OutboxLeasing_BlocksSecondWorkerUntilExpiry_AndSchedulesRetryOnTransientFailure()
    {
        if (!await CanConnectDbAsync()) return;

        var ids = OutboxFixtureIds.Create();
        var failingSender = new TestRecordingEmailSender(true, new ApplicationError("provider_timeout", "ACS timeout"));
        await SeedOutboxFixtureAsync(ids, "manual_payment.access_notification_requested");

        try
        {
            var worker = CreateWorker(failingSender);
            await worker.RunSingleBatchAsync();

            await using var db = CreateDb();
            var outboxMsg = await db.IntegrationOutboxMessages.SingleAsync(x => x.Id == ids.OutboxMessageId);
            Assert.Equal("FAILED_RETRYABLE", outboxMsg.Status);
            Assert.Equal(1, outboxMsg.AttemptCount);
            Assert.Null(outboxMsg.LeaseOwner);
            Assert.Equal("provider_timeout", outboxMsg.LastErrorCode);
        }
        finally
        {
            await CleanupOutboxFixtureAsync(ids);
        }
    }

    [Fact]
    public async Task UnconfiguredAcsSender_OutboxMessage_MarksStatusFailedRetryableAndDoesNotDeliver()
    {
        if (!await CanConnectDbAsync()) return;

        var ids = OutboxFixtureIds.Create();
        var unconfiguredOptions = Options.Create(new AzureCommunicationEmailOptions { ConnectionString = "", Endpoint = "", SenderAddress = "" });
        var unconfiguredSender = new AzureCommunicationEmailSender(unconfiguredOptions, NullLogger<AzureCommunicationEmailSender>.Instance);
        await SeedOutboxFixtureAsync(ids, "manual_payment.access_notification_requested");

        try
        {
            var worker = CreateWorker(unconfiguredSender);
            await worker.RunSingleBatchAsync();

            await using var db = CreateDb();
            var outboxMsg = await db.IntegrationOutboxMessages.SingleAsync(x => x.Id == ids.OutboxMessageId);
            Assert.Equal("FAILED_RETRYABLE", outboxMsg.Status);
            Assert.Equal(1, outboxMsg.AttemptCount);
            Assert.Null(outboxMsg.ProcessedAt);
            Assert.Null(outboxMsg.LeaseOwner);
            Assert.Equal("payment_email_not_configured", outboxMsg.LastErrorCode);
        }
        finally
        {
            await CleanupOutboxFixtureAsync(ids);
        }
    }

    private static async Task<bool> CanConnectDbAsync()
    {
        try
        {
            await using var db = CreateDb();
            return await db.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    private static EPosDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new EPosDbContext(options);
    }

    private static TestWorkerWrapper CreateWorker(IApplicationEmailSender emailSender)
    {
        var services = new ServiceCollection();
        services.AddDbContext<EPosDbContext>(opt => opt.UseNpgsql(ConnectionString));
        services.AddSingleton(emailSender);
        services.AddSingleton<ITokenHashService, TokenHashService>();
        services.AddSingleton(Options.Create(new TenantJwtOptions { SigningKey = "012345678901234567890123456789012" }));
        services.AddSingleton<IInvitationTokenService, InvitationTokenService>();
        services.AddSingleton<IManualPaymentAccessTokenService, ManualPaymentAccessTokenService>();

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var outboxOptions = Options.Create(new TenantOnboardingOutboxOptions
        {
            Enabled = true,
            PollSeconds = 1,
            BatchSize = 10,
            LeaseSeconds = 60,
            MaximumAttempts = 5,
            TenantAdminAppBaseUrl = "http://localhost:4200",
            PaymentAccessBaseUrl = "http://localhost:4200",
            ManualPaymentInstructions = "Bank: Commercial Bank, Acc: 123456789",
            PaymentSupportDetails = "billing@oneverz.test"
        });

        var worker = new TenantOnboardingOutboxWorker(scopeFactory, outboxOptions, NullLogger<TenantOnboardingOutboxWorker>.Instance);
        return new TestWorkerWrapper(worker);
    }

    private static async Task SeedOutboxFixtureAsync(OutboxFixtureIds ids, string messageType, bool setTenantActive = false)
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();

        var adminUser = PlatformUser.Create(ids.AdminUserId, ids.AdminEmail, "hash", "ACTIVE", Now);
        db.PlatformUsers.Add(adminUser);

        var plan = SubscriptionPlan.Create(ids.PlanId, ids.PlanCode, "Outbox Standard Plan",
            SubscriptionPlanConstants.Status.Active, SubscriptionPlanConstants.BillingInterval.Monthly, 150m, Now, "LKR");
        db.SubscriptionPlans.Add(plan);

        var tenant = Tenant.Create(ids.TenantId, ids.TenantCode, ids.TenantSlug, ids.TenantName,
            setTenantActive ? TenantStatusConstants.Active : TenantStatusConstants.PendingPayment,
            "LKR", "Asia/Colombo", null, null, Now);
        db.Tenants.Add(tenant);

        var primaryContact = TenantContact.Create(
            Guid.NewGuid(), ids.TenantId, "BILLING", "Primary Contact",
            ids.RecipientEmail, "+94771234567", ids.AdminUserId, Now);
        db.TenantContacts.Add(primaryContact);

        var tenantUser = TenantUser.Create(
            Guid.NewGuid(), ids.TenantId, ids.RecipientEmail, "Primary Admin",
            null, null, "pass-hash", "salt", "INVITED",
            "TENANT_ADMIN", "admin", "MAIN", Now,
            staffCode: "USR-2026-99001");
        db.TenantUsers.Add(tenantUser);

        var subscription = TenantSubscription.Create(ids.SubscriptionId, ids.TenantId, ids.PlanId, "ACTIVE", Now);
        db.TenantSubscriptions.Add(subscription);

        var invoice = SubscriptionInvoice.CreateDraft(ids.InvoiceId, ids.TenantId, ids.SubscriptionId,
            ids.InvoiceNumber, 150m, "monthly", Now.AddDays(7), "LKR", Now, Now.AddMonths(1), Now);
        db.SubscriptionInvoices.Add(invoice);

        var payment = SubscriptionPaymentTransaction.CreateAwaitingManual(ids.PaymentId, ids.TenantId,
            ids.SubscriptionId, ids.InvoiceId, 150m, "LKR", ids.InvoiceNumber, Now);
        db.SubscriptionPaymentTransactions.Add(payment);

        var payload = JsonSerializer.Serialize(new { paymentId = ids.PaymentId, accessId = ids.AccessId, tenantId = ids.TenantId });
        var outboxMessage = IntegrationOutboxMessage.Create(
            ids.OutboxMessageId, messageType, "manual_payment", ids.PaymentId,
            1, ids.TenantId, ids.CorrelationId, null, payload, $"dedupe:{ids.OutboxMessageId:N}", Now);
        db.IntegrationOutboxMessages.Add(outboxMessage);

        var draft = PlatformTenantOnboardingDraft.Create(ids.DraftId, ids.AdminUserId, "{}", 7, 127, 100, Now, Now.AddDays(30));
        db.PlatformTenantOnboardingDrafts.Add(draft);

        var operation = PlatformTenantOnboardingOperation.CreateCompleted(
            ids.OperationId, ids.DraftId, ids.TenantId, "hash-1", "hash-2",
            "AWAITING_PAYMENT", "NOT_ELIGIBLE", Now);
        db.PlatformTenantOnboardingOperations.Add(operation);

        await db.SaveChangesAsync();
    }

    private static async Task CleanupOutboxFixtureAsync(OutboxFixtureIds ids)
    {
        await using var db = CreateDb();

        var outbox = await db.IntegrationOutboxMessages.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.IntegrationOutboxMessages.RemoveRange(outbox);

        var invites = await db.UserInvites.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.UserInvites.RemoveRange(invites);

        var ops = await db.PlatformTenantOnboardingOperations.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.PlatformTenantOnboardingOperations.RemoveRange(ops);

        var drafts = await db.PlatformTenantOnboardingDrafts.Where(x => x.Id == ids.DraftId).ToListAsync();
        db.PlatformTenantOnboardingDrafts.RemoveRange(drafts);

        var links = await db.SubscriptionPaymentLinks.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.SubscriptionPaymentLinks.RemoveRange(links);

        var payments = await db.SubscriptionPaymentTransactions.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.SubscriptionPaymentTransactions.RemoveRange(payments);

        var invoices = await db.SubscriptionInvoices.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.SubscriptionInvoices.RemoveRange(invoices);

        var subs = await db.TenantSubscriptions.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.TenantSubscriptions.RemoveRange(subs);

        var users = await db.TenantUsers.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.TenantUsers.RemoveRange(users);

        var contacts = await db.TenantContacts.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.TenantContacts.RemoveRange(contacts);

        var tenants = await db.Tenants.Where(x => x.Id == ids.TenantId).ToListAsync();
        db.Tenants.RemoveRange(tenants);

        var plans = await db.SubscriptionPlans.Where(x => x.Id == ids.PlanId).ToListAsync();
        db.SubscriptionPlans.RemoveRange(plans);

        var admins = await db.PlatformUsers.Where(x => x.Id == ids.AdminUserId).ToListAsync();
        db.PlatformUsers.RemoveRange(admins);

        await db.SaveChangesAsync();
    }

    private sealed record OutboxFixtureIds(
        Guid TenantId, string TenantCode, string TenantSlug, string TenantName,
        Guid PlanId, string PlanCode, Guid SubscriptionId, Guid InvoiceId, string InvoiceNumber,
        Guid PaymentId, Guid AccessId, Guid OutboxMessageId, Guid DraftId, Guid OperationId, Guid CorrelationId,
        Guid AdminUserId, string AdminEmail, string RecipientEmail)
    {
        public static OutboxFixtureIds Create()
        {
            var id = Guid.NewGuid().ToString("N")[..8];
            return new OutboxFixtureIds(
                Guid.NewGuid(), $"TNT-{id}", $"slug-{id}", $"Outbox Tenant {id}",
                Guid.NewGuid(), $"PLAN-{id}", Guid.NewGuid(), Guid.NewGuid(), $"INV-{id}",
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), $"admin-{id}@oneverz.test", $"billing-{id}@oneverz.test");
        }
    }

    private sealed class TestRecordingEmailSender : IApplicationEmailSender
    {
        private readonly ApplicationError? _failure;
        public TestRecordingEmailSender(bool configured, ApplicationError? failure = null)
        {
            IsConfigured = configured;
            _failure = failure;
        }

        public bool IsConfigured { get; }
        public List<ApplicationEmailMessage> SentMessages { get; } = [];

        public Task<ApplicationResult<ApplicationEmailSendResult>> SendAsync(ApplicationEmailMessage message, CancellationToken cancellationToken)
        {
            if (_failure is not null)
                return Task.FromResult(ApplicationResult<ApplicationEmailSendResult>.Failure(_failure));

            SentMessages.Add(message);
            return Task.FromResult(ApplicationResult<ApplicationEmailSendResult>.Success(new ApplicationEmailSendResult("op-test-123", "Started", "op-test-123")));
        }
    }

    private sealed class TestWorkerWrapper
    {
        private readonly TenantOnboardingOutboxWorker _worker;
        public TestWorkerWrapper(TenantOnboardingOutboxWorker worker) => _worker = worker;

        public async Task RunSingleBatchAsync()
        {
            var claimMethod = typeof(TenantOnboardingOutboxWorker).GetMethod("ClaimAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var processMethod = typeof(TenantOnboardingOutboxWorker).GetMethod("ProcessAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var claimedIds = await (Task<IReadOnlyList<Guid>>)claimMethod!.Invoke(_worker, new object[] { CancellationToken.None })!;
            foreach (var id in claimedIds)
            {
                await (Task)processMethod!.Invoke(_worker, new object[] { id, CancellationToken.None })!;
            }
        }
    }
}
