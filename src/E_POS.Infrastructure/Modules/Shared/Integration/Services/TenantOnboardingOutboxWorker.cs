using System.Security.Cryptography;
using System.Text.Json;
using E_POS.Application.Common.Email;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Shared.Integration.Services;

public sealed class TenantOnboardingOutboxOptions
{
    public const string SectionName = "TenantOnboardingOutbox";
    public bool Enabled { get; init; } = true;
    public int PollSeconds { get; init; } = 5;
    public int BatchSize { get; init; } = 20;
    public int LeaseSeconds { get; init; } = 60;
    public int MaximumAttempts { get; init; } = 8;
    public int InvitationExpiryHours { get; init; } = 24;
    public string? TenantAdminAppBaseUrl { get; init; }
    public string? PaymentAccessBaseUrl { get; init; }
    public string? ManualPaymentInstructions { get; init; }
    public string? PaymentSupportDetails { get; init; }
}

public sealed class TenantOnboardingOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TenantOnboardingOutboxOptions _options;
    private readonly ILogger<TenantOnboardingOutboxWorker> _logger;
    private readonly string _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    public TenantOnboardingOutboxWorker(IServiceScopeFactory scopeFactory,
        IOptions<TenantOnboardingOutboxOptions> options, ILogger<TenantOnboardingOutboxWorker> logger)
    { _scopeFactory = scopeFactory; _options = options.Value; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var id in await ClaimAsync(stoppingToken)) await ProcessAsync(id, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { _logger.LogError(ex, "Tenant onboarding outbox polling failed. WorkerId={WorkerId}", _workerId); }
            await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(_options.PollSeconds, 1, 60)), stoppingToken);
        }
    }

    private async Task<IReadOnlyList<Guid>> ClaimAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EPosDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var batch = Math.Clamp(_options.BatchSize, 1, 100);
        var messages = await db.IntegrationOutboxMessages.FromSqlInterpolated($@"SELECT * FROM integration_outbox_messages
            WHERE status IN ('PENDING','FAILED_RETRYABLE','PROCESSING') AND available_at <= {now}
              AND (status <> 'PROCESSING' OR lease_expires_at IS NULL OR lease_expires_at <= {now})
            ORDER BY available_at, created_at FOR UPDATE SKIP LOCKED LIMIT {batch}").ToListAsync(ct);
        foreach (var message in messages)
            message.TryAcquire(_workerId, now, TimeSpan.FromSeconds(Math.Clamp(_options.LeaseSeconds, 15, 600)));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return messages.Select(x => x.Id).ToArray();
    }

    private async Task ProcessAsync(Guid messageId, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EPosDbContext>();
        var message = await db.IntegrationOutboxMessages.SingleOrDefaultAsync(x => x.Id == messageId, ct);
        if (message is null || message.Status != "PROCESSING" || message.LeaseOwner != _workerId) return;
        try
        {
            if (message.MessageType is "tenant_admin.invitation_requested" or "tenant_admin.invitation_resend_requested")
                await DispatchInvitationAsync(scope.ServiceProvider, db, message.AggregateId, message.TenantId!.Value, ct);
            else if (message.MessageType.StartsWith("manual_payment.", StringComparison.Ordinal))
                await DispatchManualPaymentAsync(scope.ServiceProvider, db, message, ct);
            else throw new InvalidOperationException("Unsupported outbox event type.");
            message.MarkDelivered(DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            var retryable = ex as RetryableDeliveryException;
            var terminal = message.AttemptCount >= Math.Clamp(_options.MaximumAttempts, 1, 25) || retryable is null;
            var delay = TimeSpan.FromMinutes(Math.Min(60, Math.Pow(2, Math.Max(0, message.AttemptCount - 1))));
            message.MarkFailed(retryable?.Code ?? "outbox_handler_failed", retryable?.SafeMessage ?? "Outbox handler failed.",
                terminal, DateTimeOffset.UtcNow.Add(delay), DateTimeOffset.UtcNow);
            _logger.LogWarning("Outbox delivery failed. MessageId={MessageId} Type={MessageType} Attempt={Attempt} Code={Code}",
                message.Id, message.MessageType, message.AttemptCount, message.LastErrorCode);
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task DispatchInvitationAsync(IServiceProvider services, EPosDbContext db, Guid operationId, Guid tenantId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.TenantAdminAppBaseUrl))
            throw new RetryableDeliveryException("invitation_base_url_not_configured", "Tenant Admin application URL is not configured.");
        var sender = services.GetRequiredService<IApplicationEmailSender>();
        if (!sender.IsConfigured) throw new RetryableDeliveryException("invitation_email_not_configured", "Invitation email provider is not configured.");
        var tenant = await db.Tenants.SingleAsync(x => x.Id == tenantId, ct);
        if (!string.Equals(tenant.Status, "active", StringComparison.OrdinalIgnoreCase))
            throw new RetryableDeliveryException("tenant_not_activation_eligible", "Tenant is not activation eligible.");
        var user = await db.TenantUsers.Where(x => x.TenantId == tenantId && x.AccountStatus == "INVITED")
            .OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(ct) ?? throw new InvalidOperationException("Tenant Admin membership is missing.");
        var roleId = await db.TenantUserRoles.Where(x => x.TenantId == tenantId && x.TenantUserId == user.Id)
            .Select(x => (Guid?)x.TenantRoleId).FirstOrDefaultAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var oldInvites = await db.UserInvites.Where(x => x.TenantId == tenantId && x.NormalizedInvitedEmail == user.Email &&
            (x.InviteStatus == "PENDING" || x.InviteStatus == "SENT")).ToListAsync(ct);
        foreach (var old in oldInvites) old.Cancel(now);
        var rawToken = ToBase64Url(RandomNumberGenerator.GetBytes(32));
        var signingKey = services.GetRequiredService<IOptions<TenantJwtOptions>>().Value.SigningKey;
        if (string.IsNullOrWhiteSpace(signingKey)) throw new RetryableDeliveryException("invitation_hash_key_not_configured", "Invitation hashing is not configured.");
        var hash = services.GetRequiredService<ITokenHashService>().HashToken(rawToken, signingKey);
        var invite = UserInvite.CreatePending(Guid.NewGuid(), tenantId, user.Email, user.Email, roleId, null, hash,
            now.AddHours(Math.Clamp(_options.InvitationExpiryHours, 1, 168)), now);
        db.UserInvites.Add(invite);
        await db.SaveChangesAsync(ct);
        var url = $"{_options.TenantAdminAppBaseUrl.TrimEnd('/')}/setup-account?token={Uri.EscapeDataString(rawToken)}";
        var send = await sender.SendAsync(new ApplicationEmailMessage(user.Email, "Set up your Tenant Admin account",
            $"<p>Your tenant is ready.</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(url)}\">Set up account</a></p>",
            "Your tenant is ready. Use the secure setup link in this email.", operationId.ToString("D")), ct);
        if (send.IsFailure) throw new RetryableDeliveryException(send.Error.Code, "Invitation provider rejected the message.");
        invite.MarkSent(DateTimeOffset.UtcNow);
        var operation = await db.PlatformTenantOnboardingOperations.SingleAsync(x => x.Id == operationId, ct);
        operation.MarkInvitationSent(DateTimeOffset.UtcNow);
    }

    private async Task DispatchManualPaymentAsync(IServiceProvider services, EPosDbContext db,
        E_POS.Domain.Modules.Shared.Integration.Entities.IntegrationOutboxMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.PaymentAccessBaseUrl))
            throw new RetryableDeliveryException("payment_access_base_url_not_configured", "Payment access URL is not configured.");
        if (string.IsNullOrWhiteSpace(_options.ManualPaymentInstructions))
            throw new RetryableDeliveryException("payment_instructions_not_configured", "Manual payment instructions are not configured.");
        var sender = services.GetRequiredService<IApplicationEmailSender>();
        if (!sender.IsConfigured)
            throw new RetryableDeliveryException("payment_email_not_configured", "Payment notification provider is not configured.");

        Guid paymentId;
        Guid? requestedAccessId = null;
        using (var payload = JsonDocument.Parse(message.PayloadJson))
        {
            var root = payload.RootElement;
            paymentId = root.TryGetProperty("paymentId", out var p) && p.TryGetGuid(out var parsed)
                ? parsed : message.AggregateId;
            if (root.TryGetProperty("accessId", out var a) && a.ValueKind != JsonValueKind.Null && a.TryGetGuid(out var accessId))
                requestedAccessId = accessId;
        }

        var payment = await db.SubscriptionPaymentTransactions.SingleOrDefaultAsync(x => x.Id == paymentId, ct)
            ?? throw new InvalidOperationException("Manual payment is missing.");
        var invoice = await db.SubscriptionInvoices.SingleAsync(x => x.Id == payment.InvoiceId, ct);
        var subscription = await db.TenantSubscriptions.SingleAsync(x => x.Id == invoice.SubscriptionId, ct);
        var tenant = await db.Tenants.AsNoTracking().SingleAsync(x => x.Id == payment.TenantId, ct);
        var planName = await db.SubscriptionPlans.AsNoTracking().Where(x => x.Id == subscription.SubscriptionPlanId)
            .Select(x => x.Name).SingleAsync(ct);
        var recipient = subscription.InvoiceEmail;
        if (string.IsNullOrWhiteSpace(recipient))
            recipient = await db.TenantUsers.AsNoTracking().Where(x => x.TenantId == payment.TenantId)
                .OrderBy(x => x.CreatedAt).Select(x => x.Email).FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(recipient))
            throw new RetryableDeliveryException("payment_recipient_missing", "Payment notification recipient is missing.");

        SubscriptionPaymentLink? access = requestedAccessId.HasValue
            ? await db.SubscriptionPaymentLinks.SingleOrDefaultAsync(x => x.Id == requestedAccessId.Value &&
                x.PaymentTransactionId == payment.Id, ct)
            : await db.SubscriptionPaymentLinks.Where(x => x.PaymentTransactionId == payment.Id && x.RevokedAt == null)
                .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        var now = DateTimeOffset.UtcNow;
        if (access is null || access.ExpiresAt <= now)
        {
            access?.Revoke(now);
            access = SubscriptionPaymentLink.CreateManualAccess(Guid.NewGuid(), payment.TenantId, payment.InvoiceId,
                payment.Id, Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
                    recipient.Trim().ToUpperInvariant()))).ToLowerInvariant(), now.AddDays(14), now);
            db.SubscriptionPaymentLinks.Add(access);
        }

        var tokenService = services.GetRequiredService<IManualPaymentAccessTokenService>();
        var rawToken = tokenService.GenerateToken();
        access.ProvisionToken(tokenService.HashToken(rawToken), recipient, now);
        // Persist the hash before delivery so a successfully delivered link remains usable
        // even if the worker terminates before it can mark the outbox message delivered.
        await db.SaveChangesAsync(ct);
        var url = $"{_options.PaymentAccessBaseUrl.TrimEnd('/')}/api/v1/tenant-onboarding/payment-access/{Uri.EscapeDataString(rawToken)}";
        var invoiceUrl = $"{url}/invoice";
        var status = payment.TransactionStatus.Replace('_', ' ').ToLowerInvariant();
        var subject = message.MessageType switch
        {
            "manual_payment.access_notification_requested" => $"Payment required for invoice {invoice.InvoiceNumber}",
            "manual_payment.submitted_notification_requested" => $"Payment submission received for {invoice.InvoiceNumber}",
            "manual_payment.approved_notification_requested" => $"Payment approved for {invoice.InvoiceNumber}",
            "manual_payment.rejected_notification_requested" => $"Payment review update for {invoice.InvoiceNumber}",
            "manual_payment.action_required_notification_requested" => $"Payment information required for {invoice.InvoiceNumber}",
            _ => $"Payment update for invoice {invoice.InvoiceNumber}"
        };
        var safeUrl = System.Net.WebUtility.HtmlEncode(url);
        var safeInvoiceUrl = System.Net.WebUtility.HtmlEncode(invoiceUrl);
        Func<string, string> encode = value => System.Net.WebUtility.HtmlEncode(value);
        var support = string.IsNullOrWhiteSpace(_options.PaymentSupportDetails)
            ? string.Empty : $"<p>Support: {encode(_options.PaymentSupportDetails!)}</p>";
        var send = await sender.SendAsync(new ApplicationEmailMessage(recipient, subject,
            $"<p>Tenant: {encode(tenant.DisplayName)} ({encode(tenant.TenantCode)})</p>" +
            $"<p>Plan: {encode(planName)}; billing cycle: {encode(subscription.BillingCycle ?? "not set")}</p>" +
            $"<p>Invoice {encode(invoice.InvoiceNumber)}; amount {invoice.TotalAmount:0.00} {encode(invoice.CurrencyCode)}; " +
            $"tax {invoice.TaxAmount:0.00}; due {encode(invoice.DueAt?.ToString("yyyy-MM-dd") ?? "not set")}</p>" +
            $"<p>Status: {encode(status)}</p><p>{encode(_options.ManualPaymentInstructions!)}</p>" +
            $"<p><a href=\"{safeInvoiceUrl}\">View invoice</a></p>" +
            $"<p><a href=\"{safeUrl}\">View payment status</a></p>{support}",
            $"Manual payment for {tenant.TenantCode}, invoice {invoice.InvoiceNumber}: {invoice.TotalAmount:0.00} " +
            $"{invoice.CurrencyCode}. Status: {status}. Use the secure invoice and payment-status links in this email.",
            message.CorrelationId.ToString("D")), ct);
        if (send.IsFailure)
            throw new RetryableDeliveryException(send.Error.Code, "Payment notification provider rejected the message.");
    }

    private static string ToBase64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private sealed class RetryableDeliveryException(string code, string safeMessage) : Exception(safeMessage)
    { public string Code { get; } = code; public string SafeMessage { get; } = safeMessage; }
}
