using Azure.Storage.Blobs;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Infrastructure.Modules.Shared.Media.Options;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;

public sealed class AzureManualPaymentEvidenceStorage : IManualPaymentEvidenceStorage
{
    private readonly IMediaObjectStorage _storage;
    private readonly AzureBlobStorageOptions _options;

    public AzureManualPaymentEvidenceStorage(IMediaObjectStorage storage, IOptions<AzureBlobStorageOptions> options)
    {
        _storage = storage;
        _options = options.Value;
    }

    public bool IsConfigured => _storage.IsConfigured;

    public async Task<ManualPaymentStoredObject> UploadAsync(Guid tenantId, Guid paymentId, Guid evidenceId,
        string safeFileName, Stream content, string contentType, IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        var key = $"manual-payments/{tenantId:D}/{paymentId:D}/{evidenceId:D}/{safeFileName}";
        var result = await _storage.UploadAsync(new MediaObjectUploadRequest(key, content, contentType, metadata), cancellationToken);
        return new(result.ContainerName, result.StorageKey);
    }

    public async Task<Stream> OpenReadAsync(string container, string storageKey, CancellationToken cancellationToken)
    {
        if (!IsConfigured) throw new InvalidOperationException("Private payment evidence storage is not configured.");
        var client = new BlobContainerClient(_options.ConnectionString, container).GetBlobClient(storageKey);
        var response = await client.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public Task DeleteIfExistsAsync(string container, string storageKey, CancellationToken cancellationToken) =>
        _storage.DeleteIfExistsAsync(container, storageKey, cancellationToken);
}

public sealed class UnavailableManualPaymentEvidenceScanner : IManualPaymentEvidenceScanner
{
    public Task<string> ScanAsync(Stream content, string contentType, CancellationToken cancellationToken) =>
        Task.FromResult(E_POS.Domain.Modules.Platform.Subscription.Constants.ManualPaymentConstants.ScanUnavailable);
}

public sealed class ManualPaymentProvider : IPaymentProvider
{
    public string ProviderType => "MANUAL";
    public PaymentProviderCapabilities Capabilities => new(false, false, false, false);

    public Task<PaymentSessionResult> CreateSessionAsync(PaymentSessionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentSessionResult(null, null, new("AWAITING_PAYMENT")));

    public Task<PaymentProviderStatus> GetStatusAsync(string providerPaymentId, CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentProviderStatus("AWAITING_PAYMENT"));

    public Task<PaymentProviderCallbackResult> VerifyCallbackAsync(PaymentProviderCallbackRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Manual payment does not accept provider callbacks.");

    public Task CancelAsync(string providerPaymentId, string idempotencyKey, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Manual payment does not support provider cancellation.");

    public Task RefundAsync(string providerPaymentId, decimal amount, string currencyCode, string idempotencyKey, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Manual payment does not support provider refunds.");

    public string MapProviderStatus(string providerStatus) => ManualPaymentConstants.AwaitingPayment;
}
