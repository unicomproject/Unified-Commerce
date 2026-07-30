using Azure.Communication.Email;
using Azure.Identity;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Infrastructure.Integrations.Email;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;

public sealed class PlatformDashboardHealthProbe : IPlatformDashboardHealthProbe
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

    private readonly EPosDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly AzureCommunicationEmailOptions _emailOptions;
    private readonly ILogger<PlatformDashboardHealthProbe> _logger;

    public PlatformDashboardHealthProbe(
        EPosDbContext dbContext,
        IConfiguration configuration,
        IOptions<AzureCommunicationEmailOptions> emailOptions,
        ILogger<PlatformDashboardHealthProbe> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task<PlatformDashboardSystemHealthDto> ProbeAsync(CancellationToken cancellationToken)
    {
        var checkedAt = DateTimeOffset.UtcNow;
        var dependencies = new List<PlatformDashboardHealthDependencyDto>
        {
            new("core_api", "HEALTHY", IsCritical: true, Message: null),
            await ProbeDatabaseAsync(cancellationToken),
            await ProbeBackgroundJobsAsync(cancellationToken),
            await ProbeEmailAsync(cancellationToken),
            ProbePayment(),
            await ProbeBlobAsync(cancellationToken)
        };

        return new PlatformDashboardSystemHealthDto(
            PlatformDashboardHealthAggregator.Aggregate(dependencies),
            checkedAt,
            dependencies);
    }

    private async Task<PlatformDashboardHealthDependencyDto> ProbeDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ProbeTimeout);
            var canConnect = await _dbContext.Database.CanConnectAsync(timeoutCts.Token);
            return canConnect
                ? new("database", "HEALTHY", true, null)
                : new("database", "CRITICAL", true, "Database unavailable.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Platform dashboard database health probe failed.");
            return new("database", "CRITICAL", true, "Database unavailable.");
        }
    }

    private async Task<PlatformDashboardHealthDependencyDto> ProbeBackgroundJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ProbeTimeout);
            // No dedicated worker heartbeat table yet — probe recent successful password-reset token activity
            // as a lightweight operational signal that async/platform processing pathways are usable.
            var recentCutoff = DateTimeOffset.UtcNow.AddHours(-24);
            var recentResetActivity = await _dbContext.PlatformPasswordResetTokens
                .AsNoTracking()
                .AnyAsync(x => x.CreatedAt >= recentCutoff || (x.UsedAt != null && x.UsedAt >= recentCutoff), timeoutCts.Token);

            if (recentResetActivity)
            {
                return new("background_jobs", "HEALTHY", false, null);
            }

            return new(
                "background_jobs",
                "UNKNOWN",
                false,
                "No recent background-job heartbeat signals were observed.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Platform dashboard background-job health probe failed.");
            return new("background_jobs", "UNKNOWN", false, "Background job health could not be determined.");
        }
    }

    private async Task<PlatformDashboardHealthDependencyDto> ProbeEmailAsync(CancellationToken cancellationToken)
    {
        var hasConnectionString = !string.IsNullOrWhiteSpace(_emailOptions.ConnectionString);
        var hasEndpoint = !string.IsNullOrWhiteSpace(_emailOptions.Endpoint);
        if (!hasConnectionString && !hasEndpoint)
        {
            return new(
                "email",
                "DEGRADED",
                false,
                _emailOptions.AllowAdminSecureLinkFallback
                    ? "Email transport is not configured (admin secure-link fallback only)."
                    : "Email transport is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_emailOptions.SenderAddress))
        {
            return new("email", "DEGRADED", false, "Email sender address is not configured.");
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ProbeTimeout);
            await Task.Run(() =>
            {
                // Constructing the ACS client validates credentials/endpoint shape without sending mail.
                // Config/shape validation alone must never be reported as HEALTHY.
                _ = hasConnectionString
                    ? new EmailClient(_emailOptions.ConnectionString)
                    : new EmailClient(new Uri(_emailOptions.Endpoint!), new DefaultAzureCredential());
            }, timeoutCts.Token);

            return new(
                "email",
                "UNKNOWN",
                false,
                "Email provider is configured but no safe live connectivity probe is available.");
        }
        catch (OperationCanceledException)
        {
            return new("email", "DEGRADED", false, "Email provider probe timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Platform dashboard email health probe failed.");
            return new("email", "DEGRADED", false, "Email provider is unreachable.");
        }
    }

    private PlatformDashboardHealthDependencyDto ProbePayment()
    {
        // No non-destructive live payment adapter is registered in this solution.
        // Config presence alone must never be reported as HEALTHY.
        var configured = FirstConfigured(
            "Payments:Provider",
            "Payment:Provider",
            "Stripe:SecretKey",
            "PayHere:MerchantId");

        if (string.IsNullOrWhiteSpace(configured))
        {
            return new("payment", "DEGRADED", true, "Payment provider is not configured.");
        }

        return new(
            "payment",
            "UNKNOWN",
            true,
            "Payment provider is configured but no safe live probe is available.");
    }

    private async Task<PlatformDashboardHealthDependencyDto> ProbeBlobAsync(CancellationToken cancellationToken)
    {
        var connection = FirstConfigured(
            "AzureBlobStorage:ConnectionString",
            "Storage:ConnectionString",
            "BlobStorage:ConnectionString",
            "ConnectionStrings:BlobStorage");
        var endpoint = FirstConfigured("AzureBlobStorage:Endpoint", "Storage:Endpoint", "BlobStorage:Endpoint");

        if (string.IsNullOrWhiteSpace(connection) && string.IsNullOrWhiteSpace(endpoint))
        {
            return new("blob", "DEGRADED", false, "Blob storage is not configured.");
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ProbeTimeout);

            // Lightweight connectivity: HTTP HEAD/GET against the configured endpoint host when available.
            // Avoid uploading objects on every Dashboard request.
            if (!string.IsNullOrWhiteSpace(endpoint) &&
                Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                using var client = new HttpClient { Timeout = ProbeTimeout };
                using var request = new HttpRequestMessage(HttpMethod.Head, uri);
                using var response = await client.SendAsync(request, timeoutCts.Token);
                return response.IsSuccessStatusCode || (int)response.StatusCode is >= 200 and < 500
                    ? new("blob", "HEALTHY", false, null)
                    : new("blob", "DEGRADED", false, "Blob storage endpoint returned an error status.");
            }

            // Connection-string-only environments: parse account endpoint and probe it.
            if (!string.IsNullOrWhiteSpace(connection))
            {
                var accountUri = TryExtractBlobUri(connection);
                if (accountUri is null)
                {
                    return new("blob", "UNKNOWN", false, "Blob connection string could not be probed safely.");
                }

                using var client = new HttpClient { Timeout = ProbeTimeout };
                using var request = new HttpRequestMessage(HttpMethod.Head, accountUri);
                using var response = await client.SendAsync(request, timeoutCts.Token);
                return (int)response.StatusCode is >= 200 and < 500
                    ? new("blob", "HEALTHY", false, null)
                    : new("blob", "DEGRADED", false, "Blob storage endpoint returned an error status.");
            }

            return new("blob", "UNKNOWN", false, "Blob storage health could not be determined.");
        }
        catch (OperationCanceledException)
        {
            return new("blob", "DEGRADED", false, "Blob storage probe timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Platform dashboard blob health probe failed.");
            return new("blob", "DEGRADED", false, "Blob storage is unreachable.");
        }
    }

    private static Uri? TryExtractBlobUri(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0)
            {
                continue;
            }

            var key = part[..idx];
            var value = part[(idx + 1)..];
            if (string.Equals(key, "BlobEndpoint", StringComparison.OrdinalIgnoreCase) &&
                Uri.TryCreate(value, UriKind.Absolute, out var blobUri))
            {
                return blobUri;
            }

            if (string.Equals(key, "AccountName", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return new Uri($"https://{value}.blob.core.windows.net/");
            }
        }

        return null;
    }

    private string? FirstConfigured(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = _configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

}
