using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class ManualPaymentServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly string Token = new('t', 43);

    [Fact]
    public async Task Queue_WithoutBillingView_IsDeniedBeforeRepositoryAccess()
    {
        var fixture = Fixture.Create();

        var result = await fixture.Service.GetQueueAsync(new(), Guid.NewGuid(), default);

        Assert.True(result.IsFailure);
        Assert.Equal("manual_payment.access_denied", result.Error.Code);
        Assert.False(fixture.Repository.QueueCalled);
    }

    [Fact]
    public async Task Review_WithoutBillingManage_IsDeniedBeforeMutation()
    {
        var fixture = Fixture.Create();

        var result = await fixture.Service.ReviewAsync(Guid.NewGuid(), new(ManualPaymentConstants.Approve, 2),
            "review-key", Guid.NewGuid(), Guid.NewGuid(), default);

        Assert.True(result.IsFailure);
        Assert.Equal("manual_payment.access_denied", result.Error.Code);
        Assert.False(fixture.Repository.ReviewCalled);
    }

    [Fact]
    public async Task Submit_CurrencyMismatch_IsRejectedBeforeScanStorageOrDatabase()
    {
        var fixture = Fixture.Create(context: AccessContext());
        var result = await fixture.Service.SubmitAsync(Token,
            Request(currency: "EUR"), Upload(), "submission-key", Guid.NewGuid(), default);

        Assert.True(result.IsFailure);
        Assert.Equal("manual_payment.currency_mismatch", result.Error.Code);
        Assert.False(fixture.Scanner.Called);
        Assert.False(fixture.Storage.UploadCalled);
        Assert.False(fixture.Repository.SubmitCalled);
    }

    [Fact]
    public async Task Submit_MalwarePositiveProof_IsRejectedBeforePrivateStorage()
    {
        var fixture = Fixture.Create(context: AccessContext(), scanStatus: ManualPaymentConstants.ScanRejected);
        var result = await fixture.Service.SubmitAsync(Token,
            Request(), Upload(), "submission-key", Guid.NewGuid(), default);

        Assert.True(result.IsFailure);
        Assert.Equal("manual_payment.evidence_rejected", result.Error.Code);
        Assert.True(fixture.Scanner.Called);
        Assert.False(fixture.Storage.UploadCalled);
        Assert.False(fixture.Repository.SubmitCalled);
    }

    [Fact]
    public async Task Submit_CleanProof_UsesPrivateStorageAndNormalizedCommand()
    {
        var fixture = Fixture.Create(context: AccessContext(), scanStatus: ManualPaymentConstants.ScanClean);
        var result = await fixture.Service.SubmitAsync(Token,
            Request(reference: " bank-0001 "), Upload(), "submission-key", Guid.NewGuid(), default);

        Assert.True(result.IsSuccess);
        Assert.True(fixture.Storage.UploadCalled);
        Assert.True(fixture.Repository.SubmitCalled);
        Assert.NotNull(fixture.Repository.LastSubmit);
        Assert.Equal("BANK_TRANSFER", fixture.Repository.LastSubmit!.PaymentMethod);
        Assert.Equal("bank-0001", fixture.Repository.LastSubmit.Reference);
        Assert.Equal("CLEAN", fixture.Repository.LastSubmit.Evidence.ScanStatus);
        Assert.Equal(64, fixture.Repository.LastSubmit.Evidence.Sha256.Length);
    }

    [Fact]
    public async Task RecipientAccess_InvalidToken_ReturnsPrivacySafeError()
    {
        var fixture = Fixture.Create();
        var result = await fixture.Service.GetStatusAsync("short", default);

        Assert.True(result.IsFailure);
        Assert.Equal("manual_payment.access_invalid_or_expired", result.Error.Code);
    }

    private static SubmitManualPaymentEvidenceRequest Request(string currency = "USD", string reference = "BANK-0001") =>
        new("bank_transfer", reference, 125m, currency, Now.AddDays(-1), "Paid", 1);

    private static ManualPaymentEvidenceUpload Upload() =>
        new(new MemoryStream("%PDF-1.7 test proof"u8.ToArray()), "proof.pdf", "application/pdf", 19);

    private static ManualPaymentAccessContext AccessContext()
    {
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var tenant = Tenant.Create(tenantId, "TEN-MP", "ten-mp", "Manual Tenant",
            TenantStatusConstants.PendingPayment, "USD", "UTC", null, null, Now);
        var subscription = TenantSubscription.Create(subscriptionId, tenantId, planId, "ACTIVE", "monthly",
            null, null, Now, Now.AddMonths(1), false, null, null, 0, "billing@example.test", "manual",
            null, null, null, null, "USD", 125m, Now, Now, Now.AddMonths(1), Guid.NewGuid(), Now);
        var invoice = SubscriptionInvoice.CreateDraft(invoiceId, tenantId, subscriptionId, "INV-MP-1", 125m,
            "monthly", Now.AddDays(7), "USD", Now, Now.AddMonths(1), Now);
        var payment = SubscriptionPaymentTransaction.CreateAwaitingManual(paymentId, tenantId, subscriptionId,
            invoiceId, 125m, "USD", "MANUAL-1", Now);
        var access = SubscriptionPaymentLink.CreateManualAccess(Guid.NewGuid(), tenantId, invoiceId, paymentId,
            new string('a', 64), Now.AddDays(14), Now);
        access.ProvisionToken(new string('b', 64), "billing@example.test", Now);
        return new(access, payment, invoice, tenant, subscription, "Pro", null);
    }

    private sealed record Fixture(ManualPaymentService Service, FakeRepository Repository,
        FakeStorage Storage, FakeScanner Scanner)
    {
        public static Fixture Create(ManualPaymentAccessContext? context = null,
            string scanStatus = ManualPaymentConstants.ScanClean)
        {
            var repository = new FakeRepository { Context = context };
            var storage = new FakeStorage();
            var scanner = new FakeScanner(scanStatus);
            var service = new ManualPaymentService(repository, new FakeTokens(), storage, scanner,
                new FakePermissions(), new FakeClock());
            return new(service, repository, storage, scanner);
        }
    }

    private sealed class FakeRepository : IManualPaymentRepository
    {
        public ManualPaymentAccessContext? Context { get; init; }
        public bool QueueCalled { get; private set; }
        public bool SubmitCalled { get; private set; }
        public bool ReviewCalled { get; private set; }
        public ManualPaymentSubmitCommand? LastSubmit { get; private set; }
        public Task<ManualPaymentAccessContext?> FindAccessAsync(string tokenHash, string action, DateTimeOffset now, CancellationToken ct) => Task.FromResult(Context);
        public Task RecordAccessAsync(Guid accessId, DateTimeOffset now, CancellationToken ct) => Task.CompletedTask;
        public Task<ManualPaymentStatusResponse?> GetStatusAsync(Guid paymentId, string accessToken, CancellationToken ct) => Task.FromResult<ManualPaymentStatusResponse?>(null);
        public Task<ManualPaymentSubmitResult> SubmitAsync(ManualPaymentSubmitCommand command, CancellationToken ct)
        {
            SubmitCalled = true;
            LastSubmit = command;
            return Task.FromResult(new ManualPaymentSubmitResult(ManualPaymentMutationOutcome.Success,
                new(command.PaymentId, ManualPaymentConstants.PaymentSubmitted, 2, "***0001", 125m, 125m,
                    "USD", command.PaymentDate, [], command.Now, command.Now, "WAIT_FOR_REVIEW")));
        }
        public Task<ManualPaymentReviewHistoryResponse?> GetHistoryAsync(Guid paymentId, bool includeActor, CancellationToken ct) => Task.FromResult<ManualPaymentReviewHistoryResponse?>(null);
        public Task<ManualPaymentQueueResponse> GetQueueAsync(ManualPaymentQueueQuery query, CancellationToken ct)
        {
            QueueCalled = true;
            return Task.FromResult(new ManualPaymentQueueResponse([], 1, 20, 0, 0));
        }
        public Task<ManualPaymentDetailResponse?> GetDetailAsync(Guid paymentId, CancellationToken ct) => Task.FromResult<ManualPaymentDetailResponse?>(null);
        public Task<SubscriptionPaymentEvidence?> GetEvidenceAsync(Guid paymentId, Guid evidenceId, CancellationToken ct) => Task.FromResult<SubscriptionPaymentEvidence?>(null);
        public Task RecordProofAccessAsync(Guid paymentId, Guid evidenceId, Guid actorId, Guid correlationId, DateTimeOffset now, CancellationToken ct) => Task.CompletedTask;
        public Task<ManualPaymentReviewResult> ReviewAsync(ManualPaymentReviewCommand command, CancellationToken ct)
        {
            ReviewCalled = true;
            return Task.FromResult(new ManualPaymentReviewResult(ManualPaymentMutationOutcome.NotFound));
        }
        public Task<ManualPaymentNotificationResult> ResendNotificationAsync(Guid paymentId, string notificationType,
            string? reason, string idempotencyKeyHash, string requestHash, Guid correlationId, Guid actorId,
            DateTimeOffset now, CancellationToken ct) => Task.FromResult(new ManualPaymentNotificationResult(ManualPaymentMutationOutcome.NotFound));
    }

    private sealed class FakeTokens : IManualPaymentAccessTokenService
    {
        public string GenerateToken() => Token;
        public string HashToken(string rawToken) => new('b', 64);
    }

    private sealed class FakeStorage : IManualPaymentEvidenceStorage
    {
        public bool IsConfigured => true;
        public bool UploadCalled { get; private set; }
        public Task<ManualPaymentStoredObject> UploadAsync(Guid tenantId, Guid paymentId, Guid evidenceId,
            string safeFileName, Stream content, string contentType, IReadOnlyDictionary<string, string> metadata,
            CancellationToken ct)
        {
            UploadCalled = true;
            return Task.FromResult(new ManualPaymentStoredObject("private", $"manual/{evidenceId:D}"));
        }
        public Task<Stream> OpenReadAsync(string container, string storageKey, CancellationToken ct) => Task.FromResult<Stream>(new MemoryStream());
        public Task DeleteIfExistsAsync(string container, string storageKey, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeScanner(string status) : IManualPaymentEvidenceScanner
    {
        public bool Called { get; private set; }
        public Task<string> ScanAsync(Stream content, string contentType, CancellationToken ct)
        {
            Called = true;
            return Task.FromResult(status);
        }
    }

    private sealed class FakePermissions : IPlatformPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid platformUserId, string permissionCode, CancellationToken ct) => Task.FromResult(false);
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}
