using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Options;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;
using E_POS.Infrastructure.Modules.Shared.Media.Options;
using E_POS.Infrastructure.Modules.Shared.Media.Services;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class ManualPaymentProofLifecycleIntegrationTests
{
    private static readonly string DbConnection =
        Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING") ??
        "Host=127.0.0.1;Port=55436;Database=oneverz_flow4_e2e_evidence;Username=flow4_e2e;Password=flow4_secure_test_pw_2026!";

    private const string ClamAvHost = "127.0.0.1";
    private const int ClamAvPort = 53311;

    private const string AzuriteConnection =
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10010/devstoreaccount1;";

    private const string AzuriteContainer = "manual-payment-evidence";

    private const string EicarTestString =
        @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

    private static readonly DateTimeOffset Now = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ClamAv_RealScanner_AccuratelyClassifiesCleanAndMalwareStreams()
    {
        if (!await CanConnectTcpAsync(ClamAvHost, ClamAvPort)) return;

        var options = Options.Create(new ManualPaymentEvidenceScannerOptions
        {
            Host = ClamAvHost,
            Port = ClamAvPort,
            TimeoutSeconds = 15
        });
        var scanner = new ClamAvManualPaymentEvidenceScanner(options);

        // 1. Clean PDF
        var cleanPdfBytes = "%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF"u8.ToArray();
        using var cleanStream = new MemoryStream(cleanPdfBytes);
        var cleanResult = await scanner.ScanAsync(cleanStream, "application/pdf", CancellationToken.None);
        Assert.Equal(ManualPaymentConstants.ScanClean, cleanResult);

        // 2. Clean PNG
        var cleanPngBytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82 };
        using var cleanPngStream = new MemoryStream(cleanPngBytes);
        var cleanPngResult = await scanner.ScanAsync(cleanPngStream, "image/png", CancellationToken.None);
        Assert.Equal(ManualPaymentConstants.ScanClean, cleanPngResult);

        // 3. EICAR malware standard string
        var eicarBytes = Encoding.ASCII.GetBytes(EicarTestString);
        using var eicarStream = new MemoryStream(eicarBytes);
        var eicarResult = await scanner.ScanAsync(eicarStream, "application/pdf", CancellationToken.None);
        Assert.Equal(ManualPaymentConstants.ScanRejected, eicarResult);
    }

    [Fact]
    public async Task Azurite_PrivateBlobContainer_EnforcesPrivateAccessAndUploadReadback()
    {
        if (!await CanConnectTcpAsync("127.0.0.1", 10010)) return;

        var blobOptions = Options.Create(new AzureBlobStorageOptions
        {
            ConnectionString = AzuriteConnection,
            ContainerName = AzuriteContainer,
            CreateContainerIfNotExists = true
        });

        IMediaObjectStorage mediaStorage = new AzureBlobMediaObjectStorage(blobOptions);
        var evidenceStorage = new AzureManualPaymentEvidenceStorage(mediaStorage, blobOptions);

        var tenantId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var safeFileName = $"receipt-{evidenceId:N}.pdf";
        var fileBytes = "%PDF-1.7\nTest evidence receipt content for Flow 4"u8.ToArray();

        using var uploadStream = new MemoryStream(fileBytes);
        var metadata = new Dictionary<string, string>
        {
            ["tenant_id"] = tenantId.ToString("D"),
            ["payment_id"] = paymentId.ToString("D"),
            ["evidence_id"] = evidenceId.ToString("D")
        };

        var stored = await evidenceStorage.UploadAsync(tenantId, paymentId, evidenceId, safeFileName,
            uploadStream, "application/pdf", metadata, CancellationToken.None);

        Assert.Equal(AzuriteContainer, stored.Container);
        Assert.Equal($"manual-payments/{tenantId:D}/{paymentId:D}/{evidenceId:D}/{safeFileName}", stored.StorageKey);

        // Verify container public access level is strictly None (private)
        var containerClient = new BlobContainerClient(AzuriteConnection, AzuriteContainer);
        var properties = await containerClient.GetPropertiesAsync();
        Assert.Equal(PublicAccessType.None, properties.Value.PublicAccess);

        // Read back bytes via authorized client
        await using var readStream = await evidenceStorage.OpenReadAsync(stored.Container, stored.StorageKey, CancellationToken.None);
        using var memory = new MemoryStream();
        await readStream.CopyToAsync(memory);
        Assert.Equal(fileBytes, memory.ToArray());

        // Verify unauthenticated direct access is denied / impossible without credentials
        using var http = new HttpClient();
        var directUrl = $"http://127.0.0.1:10010/devstoreaccount1/{AzuriteContainer}/{stored.StorageKey}";
        var unauthenticatedResponse = await http.GetAsync(directUrl);
        Assert.True(unauthenticatedResponse.StatusCode is System.Net.HttpStatusCode.BadRequest
                    or System.Net.HttpStatusCode.Forbidden
                    or System.Net.HttpStatusCode.NotFound);

        // Cleanup
        await evidenceStorage.DeleteIfExistsAsync(stored.Container, stored.StorageKey, CancellationToken.None);
    }

    [Fact]
    public async Task EndToEnd_ManualPayment_CleanUpload_PersistsCleanScanAndPrivateStorage()
    {
        if (!await CanConnectDbAsync() || !await CanConnectTcpAsync(ClamAvHost, ClamAvPort) || !await CanConnectTcpAsync("127.0.0.1", 10010))
            return;

        var ids = FixtureIds.Create();
        await SeedDatabaseAsync(ids);

        try
        {
            await using var db = CreateDb();
            var tokens = CreateTokenService();
            var storage = CreateStorage();
            var scanner = CreateScanner();
            var repository = new ManualPaymentRepository(db);
            var permissions = new StaticPermissions(true);
            var clock = new StaticClock(Now);

            var service = new ManualPaymentService(repository, tokens, storage, scanner, permissions, clock);

            var pdfBytes = "%PDF-1.7\nClean Bank Transfer Confirmation Receipt"u8.ToArray();
            using var uploadStream = new MemoryStream(pdfBytes);
            var upload = new ManualPaymentEvidenceUpload(uploadStream, "bank-proof.pdf", "application/pdf", pdfBytes.Length);
            var request = new SubmitManualPaymentEvidenceRequest("bank_transfer", "REF-E2E-VALID", 150m, "LKR",
                Now.AddHours(-2), "Payment completed via wire transfer", 1);

            var result = await service.SubmitAsync(ids.RawPaymentToken, request, upload, "idem-clean-1", Guid.NewGuid(), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(ManualPaymentConstants.PaymentSubmitted, result.Value.Status);
            Assert.Single(result.Value.Evidence);

            var evidence = result.Value.Evidence[0];
            Assert.Equal("CLEAN", evidence.ScanStatus);
            Assert.Equal("application/pdf", evidence.ContentType);

            // Verify database state
            var dbPayment = await db.SubscriptionPaymentTransactions.AsNoTracking().SingleAsync(x => x.Id == ids.PaymentId);
            Assert.Equal(ManualPaymentConstants.PaymentSubmitted, dbPayment.TransactionStatus);

            var dbEvidence = await db.SubscriptionPaymentEvidence.AsNoTracking().SingleAsync(x => x.PaymentId == ids.PaymentId);
            Assert.Equal("CLEAN", dbEvidence.ScanStatus);
            Assert.StartsWith($"manual-payments/{ids.TenantId:D}/{ids.PaymentId:D}/", dbEvidence.StorageKey);

            // Verify Admin can retrieve proof stream
            var adminProofResult = await service.OpenProofAsync(ids.PaymentId, dbEvidence.Id, ids.AdminUserId, CancellationToken.None);
            Assert.True(adminProofResult.IsSuccess);
            Assert.NotNull(adminProofResult.Value);
            Assert.Equal("application/pdf", adminProofResult.Value.ContentType);

            using var downloadedMemory = new MemoryStream();
            await adminProofResult.Value.Content.CopyToAsync(downloadedMemory);
            Assert.Equal(pdfBytes, downloadedMemory.ToArray());
        }
        finally
        {
            await CleanupDatabaseAsync(ids);
        }
    }

    [Fact]
    public async Task EndToEnd_ManualPayment_InfectedUpload_IsRejectedAndNeverStored()
    {
        if (!await CanConnectDbAsync() || !await CanConnectTcpAsync(ClamAvHost, ClamAvPort) || !await CanConnectTcpAsync("127.0.0.1", 10010))
            return;

        var ids = FixtureIds.Create();
        await SeedDatabaseAsync(ids);

        try
        {
            await using var db = CreateDb();
            var tokens = CreateTokenService();
            var storage = CreateStorage();
            var scanner = CreateScanner();
            var repository = new ManualPaymentRepository(db);
            var permissions = new StaticPermissions(true);
            var clock = new StaticClock(Now);

            var service = new ManualPaymentService(repository, tokens, storage, scanner, permissions, clock);

            // Upload standard EICAR malware test string
            var infectedBytes = Encoding.ASCII.GetBytes(EicarTestString);
            using var uploadStream = new MemoryStream(infectedBytes);
            var upload = new ManualPaymentEvidenceUpload(uploadStream, "infected-receipt.pdf", "application/pdf", infectedBytes.Length);
            var request = new SubmitManualPaymentEvidenceRequest("bank_transfer", "REF-MALWARE-1", 150m, "LKR",
                Now.AddHours(-2), "Infected receipt upload test", 1);

            var result = await service.SubmitAsync(ids.RawPaymentToken, request, upload, "idem-eicar-1", Guid.NewGuid(), CancellationToken.None);

            Assert.True(result.IsFailure);
            Assert.Equal("manual_payment.evidence_rejected", result.Error.Code);

            // Invariant: database payment must still be AWAITING_PAYMENT, zero evidence rows created
            var dbPayment = await db.SubscriptionPaymentTransactions.AsNoTracking().SingleAsync(x => x.Id == ids.PaymentId);
            Assert.Equal(ManualPaymentConstants.AwaitingPayment, dbPayment.TransactionStatus);

            var evidenceCount = await db.SubscriptionPaymentEvidence.AsNoTracking().CountAsync(x => x.PaymentId == ids.PaymentId);
            Assert.Equal(0, evidenceCount);
        }
        finally
        {
            await CleanupDatabaseAsync(ids);
        }
    }

    [Fact]
    public async Task CrossTenant_EvidenceAccess_ReturnsNotFoundOrDenied()
    {
        if (!await CanConnectDbAsync() || !await CanConnectTcpAsync(ClamAvHost, ClamAvPort) || !await CanConnectTcpAsync("127.0.0.1", 10010))
            return;

        var ids = FixtureIds.Create();
        var secondaryIds = FixtureIds.Create();
        await SeedDatabaseAsync(ids);
        await SeedDatabaseAsync(secondaryIds);

        try
        {
            await using var db = CreateDb();
            var tokens = CreateTokenService();
            var storage = CreateStorage();
            var scanner = CreateScanner();
            var repository = new ManualPaymentRepository(db);
            var permissions = new StaticPermissions(true);
            var clock = new StaticClock(Now);

            var service = new ManualPaymentService(repository, tokens, storage, scanner, permissions, clock);

            // Upload valid evidence for tenant A
            var pdfBytes = "%PDF-1.7\nTenant A Evidence"u8.ToArray();
            using var uploadStream = new MemoryStream(pdfBytes);
            var upload = new ManualPaymentEvidenceUpload(uploadStream, "tenant-a-proof.pdf", "application/pdf", pdfBytes.Length);
            var request = new SubmitManualPaymentEvidenceRequest("bank_transfer", "REF-TENANT-A", 150m, "LKR",
                Now.AddHours(-2), "Tenant A payment", 1);

            var submitResult = await service.SubmitAsync(ids.RawPaymentToken, request, upload, "idem-tenant-a", Guid.NewGuid(), CancellationToken.None);
            Assert.True(submitResult.IsSuccess);
            Assert.NotNull(submitResult.Value);
            var evidenceId = submitResult.Value.Evidence[0].Id;

            // Attempt to retrieve Tenant A evidence using Tenant B paymentId
            var crossTenantResult = await service.OpenProofAsync(secondaryIds.PaymentId, evidenceId, ids.AdminUserId, CancellationToken.None);
            Assert.True(crossTenantResult.IsFailure);
            Assert.Equal("manual_payment.not_found", crossTenantResult.Error.Code);
        }
        finally
        {
            await CleanupDatabaseAsync(ids);
            await CleanupDatabaseAsync(secondaryIds);
        }
    }

    [Fact]
    public async Task EndToEnd_ManualPayment_ScannerUnavailable_FailsClosed_AndRecoversWhenScannerRestored()
    {
        if (!await CanConnectDbAsync() || !await CanConnectTcpAsync(ClamAvHost, ClamAvPort) || !await CanConnectTcpAsync("127.0.0.1", 10010))
            return;

        var ids = FixtureIds.Create();
        await SeedDatabaseAsync(ids);

        try
        {
            await using var db = CreateDb();
            var tokens = CreateTokenService();
            var storage = CreateStorage();
            var unavailableScanner = new UnavailableScanner();
            var repository = new ManualPaymentRepository(db);
            var permissions = new StaticPermissions(true);
            var clock = new StaticClock(Now);

            var serviceWithOutage = new ManualPaymentService(repository, tokens, storage, unavailableScanner, permissions, clock);

            var pdfBytes = "%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF"u8.ToArray();
            using var uploadStream = new MemoryStream(pdfBytes);
            var upload = new ManualPaymentEvidenceUpload(uploadStream, "clean-receipt.pdf", "application/pdf", pdfBytes.Length);
            var request = new SubmitManualPaymentEvidenceRequest("bank_transfer", "REF-OUTAGE-1", 150m, "LKR",
                Now.AddHours(-2), "Clean receipt during outage", 1);

            // 1. Recipient submits when scanner is unavailable
            var submitResult = await serviceWithOutage.SubmitAsync(ids.RawPaymentToken, request, upload, "idem-outage-1", Guid.NewGuid(), CancellationToken.None);
            Assert.True(submitResult.IsSuccess);
            Assert.Equal(ManualPaymentConstants.ScanUnavailable, submitResult.Value!.Evidence[0].ScanStatus);

            // 2. Admin attempts to APPROVE evidence that is not clean -> FAIL-CLOSED
            var approveRequest = new ManualPaymentReviewRequest("APPROVE", 2, "Attempt approval during scan outage", "PAYMENT_NOT_VERIFIED");
            var approveResult = await serviceWithOutage.ReviewAsync(ids.PaymentId, approveRequest, "idem-review-outage-1", Guid.NewGuid(), ids.AdminUserId, CancellationToken.None);

            Assert.True(approveResult.IsFailure);
            Assert.Equal("manual_payment.payment_evidence_not_scanned", approveResult.Error.Code);

            // 3. Payment remains unapproved in DB
            var dbPayment = await db.SubscriptionPaymentTransactions.AsNoTracking().SingleAsync(x => x.Id == ids.PaymentId);
            Assert.NotEqual(ManualPaymentConstants.Paid, dbPayment.TransactionStatus);

            // 4. Scanner recovers -> Healthy ClamAV scanner
            var healthyScanner = CreateScanner();
            var healthyService = new ManualPaymentService(repository, tokens, storage, healthyScanner, permissions, clock);

            // Resubmit / update evidence with healthy scanner
            using var uploadStream2 = new MemoryStream(pdfBytes);
            var upload2 = new ManualPaymentEvidenceUpload(uploadStream2, "clean-receipt.pdf", "application/pdf", pdfBytes.Length);
            var updateRequest = new UpdateManualPaymentSubmissionRequest("bank_transfer", "REF-OUTAGE-1", 150m, "LKR", Now.AddHours(-2), "Clean receipt during outage", 2);
            var resubmitResult = await healthyService.UpdateAsync(ids.RawPaymentToken, ids.PaymentId, updateRequest, upload2, "idem-outage-2", Guid.NewGuid(), CancellationToken.None);
            Assert.True(resubmitResult.IsSuccess);
            Assert.Equal(ManualPaymentConstants.ScanClean, resubmitResult.Value!.Evidence[0].ScanStatus);

            // 5. Admin APPROVE succeeds after recovery
            var approveRecovered = await healthyService.ReviewAsync(ids.PaymentId, new ManualPaymentReviewRequest("APPROVE", 3, null, null), "idem-review-outage-2", Guid.NewGuid(), ids.AdminUserId, CancellationToken.None);
            Assert.True(approveRecovered.IsSuccess);
            Assert.Equal(ManualPaymentConstants.Paid, approveRecovered.Value!.PaymentStatus);
            Assert.Equal("PENDING_ACTIVATION", approveRecovered.Value!.TenantStatus);
        }
        finally
        {
            await CleanupDatabaseAsync(ids);
        }
    }

    private sealed class UnavailableScanner : IManualPaymentEvidenceScanner
    {
        public Task<string> ScanAsync(Stream content, string contentType, CancellationToken cancellationToken) =>
            Task.FromResult(ManualPaymentConstants.ScanUnavailable);
    }

    private static async Task<bool> CanConnectTcpAsync(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await client.ConnectAsync(host, port, cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> CanConnectDbAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(DbConnection);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static EPosDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(DbConnection).Options);

    private static IManualPaymentAccessTokenService CreateTokenService()
    {
        ITokenHashService hash = new TokenHashService();
        var jwt = Options.Create(new TenantJwtOptions { SigningKey = "flow4_test_signing_key_at_least_32_chars_long_entropy!" });
        return new ManualPaymentAccessTokenService(hash, jwt);
    }

    private static IManualPaymentEvidenceStorage CreateStorage()
    {
        var blobOptions = Options.Create(new AzureBlobStorageOptions
        {
            ConnectionString = AzuriteConnection,
            ContainerName = AzuriteContainer,
            CreateContainerIfNotExists = true
        });
        return new AzureManualPaymentEvidenceStorage(new AzureBlobMediaObjectStorage(blobOptions), blobOptions);
    }

    private static IManualPaymentEvidenceScanner CreateScanner()
    {
        var options = Options.Create(new ManualPaymentEvidenceScannerOptions
        {
            Host = ClamAvHost,
            Port = ClamAvPort,
            TimeoutSeconds = 15
        });
        return new ClamAvManualPaymentEvidenceScanner(options);
    }

    private static async Task SeedDatabaseAsync(FixtureIds ids)
    {
        await using var db = CreateDb();
        var tokenService = CreateTokenService();
        var rawPaymentToken = "flow4_token_" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        ids.RawPaymentToken = rawPaymentToken;
        var tokenHash = tokenService.HashToken(rawPaymentToken);

        var tenant = Tenant.Create(ids.TenantId, ids.TenantCode, ids.TenantSlug, ids.TenantName,
            TenantStatusConstants.PendingPayment, "LKR", "Asia/Colombo", null, null, Now);
        var plan = SubscriptionPlan.Create(ids.PlanId, ids.PlanCode, "Pro E2E Plan",
            SubscriptionPlanConstants.Status.Active, SubscriptionPlanConstants.BillingInterval.Monthly, 150m, Now, "LKR");
        var sub = TenantSubscription.Create(ids.SubscriptionId, ids.TenantId, ids.PlanId, "ACTIVE", Now);
        var invoice = SubscriptionInvoice.CreateDraft(ids.InvoiceId, ids.TenantId, ids.SubscriptionId,
            ids.InvoiceNumber, 150m, "monthly", Now.AddDays(7), "LKR", Now, Now.AddMonths(1), Now);
        var payment = SubscriptionPaymentTransaction.CreateAwaitingManual(ids.PaymentId, ids.TenantId,
            ids.SubscriptionId, ids.InvoiceId, 150m, "LKR", ids.InvoiceNumber, Now);
        var link = SubscriptionPaymentLink.CreateManualAccess(ids.AccessId, ids.TenantId, ids.InvoiceId,
            ids.PaymentId, new string('z', 64), Now.AddDays(14), Now);
        link.ProvisionToken(tokenHash, "admin@tenant-e2e.test", Now);

        var adminUser = PlatformUser.Create(ids.AdminUserId, ids.AdminEmail, ids.AdminPasswordHash, "ACTIVE", Now);

        await db.Tenants.AddAsync(tenant);
        await db.SubscriptionPlans.AddAsync(plan);
        await db.TenantSubscriptions.AddAsync(sub);
        await db.SubscriptionInvoices.AddAsync(invoice);
        await db.SubscriptionPaymentTransactions.AddAsync(payment);
        await db.SubscriptionPaymentLinks.AddAsync(link);
        await db.PlatformUsers.AddAsync(adminUser);

        await db.SaveChangesAsync();
    }

    private static async Task CleanupDatabaseAsync(FixtureIds ids)
    {
        await using var db = CreateDb();
        var evidences = await db.SubscriptionPaymentEvidence.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.SubscriptionPaymentEvidence.RemoveRange(evidences);

        var reviews = await db.SubscriptionPaymentReviews.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.SubscriptionPaymentReviews.RemoveRange(reviews);

        var links = await db.SubscriptionPaymentLinks.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.SubscriptionPaymentLinks.RemoveRange(links);

        var payments = await db.SubscriptionPaymentTransactions.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.SubscriptionPaymentTransactions.RemoveRange(payments);

        var invoices = await db.SubscriptionInvoices.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.SubscriptionInvoices.RemoveRange(invoices);

        var subs = await db.TenantSubscriptions.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.TenantSubscriptions.RemoveRange(subs);

        var plans = await db.SubscriptionPlans.Where(x => x.Id == ids.PlanId).ToListAsync();
        db.SubscriptionPlans.RemoveRange(plans);

        var outbox = await db.IntegrationOutboxMessages.Where(x => x.TenantId == ids.TenantId).ToListAsync();
        db.IntegrationOutboxMessages.RemoveRange(outbox);

        var tenants = await db.Tenants.Where(x => x.Id == ids.TenantId).ToListAsync();
        db.Tenants.RemoveRange(tenants);

        var users = await db.PlatformUsers.Where(x => x.Id == ids.AdminUserId).ToListAsync();
        db.PlatformUsers.RemoveRange(users);

        await db.SaveChangesAsync();
    }

    private sealed record FixtureIds(
        Guid TenantId,
        string TenantCode,
        string TenantSlug,
        string TenantName,
        Guid PlanId,
        string PlanCode,
        Guid SubscriptionId,
        Guid InvoiceId,
        string InvoiceNumber,
        Guid PaymentId,
        Guid AccessId,
        Guid AdminUserId,
        string AdminEmail,
        string AdminPasswordHash)
    {
        public string RawPaymentToken { get; set; } = string.Empty;

        public static FixtureIds Create()
        {
            var seed = Guid.NewGuid().ToString("N")[..8];
            return new(
                Guid.NewGuid(),
                $"TEN-{seed}",
                $"slug-{seed}",
                $"E2E Proof Tenant {seed}",
                Guid.NewGuid(),
                $"PLAN-PROOF-{seed}",
                Guid.NewGuid(),
                Guid.NewGuid(),
                $"INV-PROOF-{seed}",
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                $"admin.{seed}@flow4proof.test",
                "PBKDF2-SHA256:100000:B3G83oiz74Jq8+Zv7ee0dw==:j1sFOiYVSHBURb3i2QO7j8v+SF3dtysiuAuc/Ww/7Ig=");
        }
    }

    private sealed class StaticPermissions(bool allowed) : IPlatformPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid platformUserId, string permissionCode, CancellationToken ct) =>
            Task.FromResult(allowed);
    }

    private sealed class StaticClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }
}
