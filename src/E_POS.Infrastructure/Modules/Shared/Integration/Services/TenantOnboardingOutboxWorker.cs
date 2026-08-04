using System.Security.Cryptography;
using E_POS.Application.Common.Email;
using E_POS.Application.Common.Security;
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
            if (message.MessageType == "tenant_admin.invitation_requested")
                await DispatchInvitationAsync(scope.ServiceProvider, db, message.AggregateId, message.TenantId!.Value, ct);
            else if (message.MessageType == "tenant.payment_link.requested")
                throw new RetryableDeliveryException("payment_provider_not_configured", "Payment provider is not configured.");
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

    private static string ToBase64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private sealed class RetryableDeliveryException(string code, string safeMessage) : Exception(safeMessage)
    { public string Code { get; } = code; public string SafeMessage { get; } = safeMessage; }
}
