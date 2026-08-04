using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed class ManualPaymentService : IManualPaymentService
{
    private const long MaximumEvidenceBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> PaymentMethods = new(StringComparer.OrdinalIgnoreCase)
        { "BANK_TRANSFER", "CASH_DEPOSIT", "MANUAL" };
    private readonly IManualPaymentRepository _repository;
    private readonly IManualPaymentAccessTokenService _tokens;
    private readonly IManualPaymentEvidenceStorage _storage;
    private readonly IManualPaymentEvidenceScanner _scanner;
    private readonly IPlatformPermissionChecker _permissions;
    private readonly IDateTimeProvider _clock;

    public ManualPaymentService(IManualPaymentRepository repository, IManualPaymentAccessTokenService tokens,
        IManualPaymentEvidenceStorage storage, IManualPaymentEvidenceScanner scanner,
        IPlatformPermissionChecker permissions, IDateTimeProvider clock)
    {
        _repository = repository;
        _tokens = tokens;
        _storage = storage;
        _scanner = scanner;
        _permissions = permissions;
        _clock = clock;
    }

    public Task<ApplicationResult<ManualPaymentStatusResponse>> GetStatusAsync(string accessToken, CancellationToken ct) =>
        ReadRecipientAsync(accessToken, "STATUS", ct);

    public Task<ApplicationResult<ManualPaymentStatusResponse>> GetInvoiceAsync(string accessToken, CancellationToken ct) =>
        ReadRecipientAsync(accessToken, "INVOICE", ct);

    public Task<ApplicationResult<ManualPaymentSubmissionResponse>> SubmitAsync(string accessToken,
        SubmitManualPaymentEvidenceRequest request, ManualPaymentEvidenceUpload upload, string idempotencyKey,
        Guid correlationId, CancellationToken ct) => SubmitCoreAsync(accessToken, null, request.PaymentMethod,
            request.BankOrTransactionReference, request.SubmittedAmount, request.CurrencyCode, request.PaymentDate,
            request.PayerNote, request.ExpectedVersion, upload, idempotencyKey, correlationId, ct);

    public Task<ApplicationResult<ManualPaymentSubmissionResponse>> UpdateAsync(string accessToken, Guid paymentId,
        UpdateManualPaymentSubmissionRequest request, ManualPaymentEvidenceUpload upload, string idempotencyKey,
        Guid correlationId, CancellationToken ct) => SubmitCoreAsync(accessToken, paymentId, request.PaymentMethod,
            request.BankOrTransactionReference, request.SubmittedAmount, request.CurrencyCode, request.PaymentDate,
            request.PayerNote, request.ExpectedVersion, upload, idempotencyKey, correlationId, ct);

    public async Task<ApplicationResult<ManualPaymentReviewHistoryResponse>> GetRecipientHistoryAsync(string accessToken, CancellationToken ct)
    {
        var context = await AccessAsync(accessToken, "HISTORY", ct);
        if (context is null) return Failure<ManualPaymentReviewHistoryResponse>("access_invalid_or_expired", "Payment access is invalid or expired.");
        await _repository.RecordAccessAsync(context.Access.Id, _clock.UtcNow, ct);
        var result = await _repository.GetHistoryAsync(context.Payment.Id, false, ct);
        return result is null ? Failure<ManualPaymentReviewHistoryResponse>("not_found", "Payment was not found.") :
            ApplicationResult<ManualPaymentReviewHistoryResponse>.Success(result);
    }

    public async Task<ApplicationResult<ManualPaymentQueueResponse>> GetQueueAsync(ManualPaymentQueueQuery query, Guid userId, CancellationToken ct)
    {
        if (!await CanView(userId, ct)) return Failure<ManualPaymentQueueResponse>("access_denied", "Platform billing access denied.");
        return ApplicationResult<ManualPaymentQueueResponse>.Success(await _repository.GetQueueAsync(query, ct));
    }

    public async Task<ApplicationResult<ManualPaymentDetailResponse>> GetTenantPaymentStatusAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        if (!await CanView(userId, ct)) return Failure<ManualPaymentDetailResponse>("access_denied", "Platform billing access denied.");
        var queue = await _repository.GetQueueAsync(new ManualPaymentQueueQuery(1, 1, TenantId: tenantId), ct);
        var payment = queue.Items.SingleOrDefault();
        if (payment is null) return Failure<ManualPaymentDetailResponse>("not_found", "Manual payment was not found.");
        var detail = await _repository.GetDetailAsync(payment.PaymentId, ct);
        return detail is null ? Failure<ManualPaymentDetailResponse>("not_found", "Manual payment was not found.") :
            ApplicationResult<ManualPaymentDetailResponse>.Success(detail);
    }

    public async Task<ApplicationResult<ManualPaymentDetailResponse>> GetDetailAsync(Guid paymentId, Guid userId, CancellationToken ct)
    {
        if (!await CanView(userId, ct)) return Failure<ManualPaymentDetailResponse>("access_denied", "Platform billing access denied.");
        var value = await _repository.GetDetailAsync(paymentId, ct);
        return value is null ? Failure<ManualPaymentDetailResponse>("not_found", "Manual payment was not found.") :
            ApplicationResult<ManualPaymentDetailResponse>.Success(value);
    }

    public async Task<ApplicationResult<ManualPaymentProofDownload>> OpenProofAsync(Guid paymentId, Guid evidenceId,
        Guid userId, CancellationToken ct)
    {
        if (!await CanView(userId, ct)) return Failure<ManualPaymentProofDownload>("proof_access_denied", "Payment proof access denied.");
        var evidence = await _repository.GetEvidenceAsync(paymentId, evidenceId, ct);
        if (evidence is null) return Failure<ManualPaymentProofDownload>("not_found", "Payment proof was not found.");
        if (!_storage.IsConfigured) return Failure<ManualPaymentProofDownload>("storage_unavailable", "Private proof storage is unavailable.");
        await _repository.RecordProofAccessAsync(paymentId, evidenceId, userId, Guid.NewGuid(), _clock.UtcNow, ct);
        var stream = await _storage.OpenReadAsync(evidence.BlobContainer, evidence.StorageKey, ct);
        return ApplicationResult<ManualPaymentProofDownload>.Success(
            new(stream, evidence.ContentType, evidence.SafeFileName, evidence.FileSize));
    }

    public async Task<ApplicationResult<ManualPaymentReviewResponse>> ReviewAsync(Guid paymentId,
        ManualPaymentReviewRequest request, string idempotencyKey, Guid correlationId, Guid userId, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(userId, PlatformPermissionCodes.BillingManage, ct))
            return Failure<ManualPaymentReviewResponse>("access_denied", "Platform billing management access denied.");
        var keyError = ValidateKey(idempotencyKey);
        if (keyError is not null) return ApplicationResult<ManualPaymentReviewResponse>.Failure(keyError);
        var action = request.Action?.Trim().ToUpperInvariant() ?? string.Empty;
        if (action is not (ManualPaymentConstants.Approve or ManualPaymentConstants.Reject or ManualPaymentConstants.RequestInformation))
            return Failure<ManualPaymentReviewResponse>("validation_failed", "Review action is invalid.");
        if (action is ManualPaymentConstants.Reject or ManualPaymentConstants.RequestInformation &&
            (string.IsNullOrWhiteSpace(request.ReviewNote) || string.IsNullOrWhiteSpace(request.ReasonCode)))
            return Failure<ManualPaymentReviewResponse>("review_note_required", "A safe reason code and review note are required.");
        if (request.ReviewNote?.Length > 1000 || request.ReasonCode?.Length > 100)
            return Failure<ManualPaymentReviewResponse>("validation_failed", "Review note or reason is too long.");
        var keyHash = Hash(idempotencyKey.Trim());
        var requestHash = Hash(JsonSerializer.Serialize(new { paymentId, action, request.ExpectedVersion,
            note = request.ReviewNote?.Trim(), reason = request.ReasonCode?.Trim().ToUpperInvariant() }));
        var result = await _repository.ReviewAsync(new(paymentId, action, request.ExpectedVersion,
            request.ReviewNote?.Trim(), request.ReasonCode?.Trim().ToUpperInvariant(), keyHash, requestHash,
            correlationId, userId, _clock.UtcNow), ct);
        return MapReview(result);
    }

    public async Task<ApplicationResult<ManualPaymentReviewHistoryResponse>> GetAdminHistoryAsync(Guid paymentId, Guid userId, CancellationToken ct)
    {
        if (!await CanView(userId, ct)) return Failure<ManualPaymentReviewHistoryResponse>("access_denied", "Platform billing access denied.");
        var result = await _repository.GetHistoryAsync(paymentId, true, ct);
        return result is null ? Failure<ManualPaymentReviewHistoryResponse>("not_found", "Manual payment was not found.") :
            ApplicationResult<ManualPaymentReviewHistoryResponse>.Success(result);
    }

    public async Task<ApplicationResult<ManualPaymentNotificationResponse>> ResendNotificationAsync(Guid paymentId,
        ResendPaymentNotificationRequest request, string idempotencyKey, Guid correlationId, Guid userId, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(userId, PlatformPermissionCodes.BillingManage, ct))
            return Failure<ManualPaymentNotificationResponse>("access_denied", "Platform billing management access denied.");
        var keyError = ValidateKey(idempotencyKey);
        if (keyError is not null) return ApplicationResult<ManualPaymentNotificationResponse>.Failure(keyError);
        var type = request.NotificationType?.Trim().ToUpperInvariant() ?? string.Empty;
        if (type is not ("PAYMENT_REQUIRED" or "SUBMISSION_RECEIVED" or "REVIEW_OUTCOME"))
            return Failure<ManualPaymentNotificationResponse>("validation_failed", "Notification type is invalid.");
        if (request.Reason?.Length > 500)
            return Failure<ManualPaymentNotificationResponse>("validation_failed", "Notification reason is too long.");
        var keyHash = Hash(idempotencyKey.Trim());
        var requestHash = Hash(JsonSerializer.Serialize(new { paymentId, type, reason = request.Reason?.Trim() }));
        var result = await _repository.ResendNotificationAsync(paymentId, type, request.Reason?.Trim(), keyHash,
            requestHash, correlationId, userId, _clock.UtcNow, ct);
        return result.Outcome switch
        {
            ManualPaymentMutationOutcome.Success or ManualPaymentMutationOutcome.Replay when result.Response is not null =>
                ApplicationResult<ManualPaymentNotificationResponse>.Success(result.Response),
            ManualPaymentMutationOutcome.NotFound => Failure<ManualPaymentNotificationResponse>("not_found", "Manual payment was not found."),
            ManualPaymentMutationOutcome.IdempotencyConflict => Failure<ManualPaymentNotificationResponse>("idempotency_conflict", "Idempotency key was reused with a different request."),
            ManualPaymentMutationOutcome.RateLimited => Failure<ManualPaymentNotificationResponse>("rate_limited", "Notification was sent too recently."),
            _ => Failure<ManualPaymentNotificationResponse>("invalid_transition", "Notification cannot be sent in the current state.")
        };
    }

    private async Task<ApplicationResult<ManualPaymentStatusResponse>> ReadRecipientAsync(string token, string action, CancellationToken ct)
    {
        var context = await AccessAsync(token, action, ct);
        if (context is null) return Failure<ManualPaymentStatusResponse>("access_invalid_or_expired", "Payment access is invalid or expired.");
        await _repository.RecordAccessAsync(context.Access.Id, _clock.UtcNow, ct);
        var value = await _repository.GetStatusAsync(context.Payment.Id, token, ct);
        return value is null ? Failure<ManualPaymentStatusResponse>("not_found", "Payment was not found.") :
            ApplicationResult<ManualPaymentStatusResponse>.Success(value);
    }

    private async Task<ApplicationResult<ManualPaymentSubmissionResponse>> SubmitCoreAsync(string token, Guid? routePaymentId,
        string method, string reference, decimal amount, string currency, DateTimeOffset paymentDate,
        string? note, long? expectedVersion, ManualPaymentEvidenceUpload upload, string idempotencyKey,
        Guid correlationId, CancellationToken ct)
    {
        var keyError = ValidateKey(idempotencyKey);
        if (keyError is not null) return ApplicationResult<ManualPaymentSubmissionResponse>.Failure(keyError);
        var context = await AccessAsync(token, "EVIDENCE", ct);
        if (context is null) return Failure<ManualPaymentSubmissionResponse>("access_invalid_or_expired", "Payment access is invalid or expired.");
        if (routePaymentId.HasValue && routePaymentId.Value != context.Payment.Id)
            return Failure<ManualPaymentSubmissionResponse>("not_found", "Payment was not found.");
        var validation = ValidateSubmission(context, method, reference, amount, currency, paymentDate, note, upload);
        if (validation is not null) return ApplicationResult<ManualPaymentSubmissionResponse>.Failure(validation);
        if (!_storage.IsConfigured) return Failure<ManualPaymentSubmissionResponse>("storage_unavailable", "Private proof storage is unavailable.");

        await using var buffer = new MemoryStream((int)upload.Length);
        await upload.Content.CopyToAsync(buffer, ct);
        if (buffer.Length != upload.Length) return Failure<ManualPaymentSubmissionResponse>("validation_failed", "Proof upload was incomplete.");
        var normalizedContentType = upload.ContentType.Trim().ToLowerInvariant();
        if (!HasValidMagic(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), normalizedContentType))
            return Failure<ManualPaymentSubmissionResponse>("invalid_evidence_type", "Proof content does not match its declared type.");
        var sha = Convert.ToHexString(SHA256.HashData(buffer.GetBuffer().AsSpan(0, (int)buffer.Length))).ToLowerInvariant();
        var keyHash = Hash(idempotencyKey.Trim());
        var requestHash = Hash(JsonSerializer.Serialize(new
        {
            paymentId = context.Payment.Id, method = method.Trim().ToUpperInvariant(), reference = reference.Trim(),
            amount, currency = currency.Trim().ToUpperInvariant(), paymentDate, note = note?.Trim(), sha
        }));
        if (context.Payment.LastCommandIdempotencyKeyHash == keyHash && context.Payment.LastCommandRequestHash != requestHash)
            return Failure<ManualPaymentSubmissionResponse>("idempotency_conflict", "Idempotency key was reused with a different request.");

        var evidenceId = Guid.NewGuid();
        var safeName = BuildSafeFileName(upload.OriginalFileName, normalizedContentType);
        var stored = default(ManualPaymentStoredObject);
        var replay = context.Payment.LastCommandIdempotencyKeyHash == keyHash;
        var scan = ManualPaymentConstants.ScanUnavailable;
        if (!replay)
        {
            buffer.Position = 0;
            scan = await _scanner.ScanAsync(buffer, normalizedContentType, ct);
            if (scan == ManualPaymentConstants.ScanRejected)
                return Failure<ManualPaymentSubmissionResponse>("evidence_rejected", "Payment proof failed security scanning.");
            buffer.Position = 0;
            stored = await _storage.UploadAsync(context.Payment.TenantId, context.Payment.Id, evidenceId, safeName,
                buffer, normalizedContentType, new Dictionary<string, string>
                {
                    ["tenantId"] = context.Payment.TenantId.ToString("D"),
                    ["paymentId"] = context.Payment.Id.ToString("D"),
                    ["sha256"] = sha
                }, ct);
        }
        else
        {
            stored = new("replay", "replay");
        }

        var command = new ManualPaymentSubmitCommand(context.Access.Id, context.Payment.Id, amount,
            currency.Trim().ToUpperInvariant(), method.Trim().ToUpperInvariant(), reference.Trim(), paymentDate,
            note?.Trim(), expectedVersion, keyHash, requestHash, correlationId,
            new(evidenceId, stored.Container, stored.StorageKey, upload.OriginalFileName, safeName,
                normalizedContentType, upload.Length, sha, scan), _clock.UtcNow);
        var result = await _repository.SubmitAsync(command, ct);
        if (!replay && result.Outcome != ManualPaymentMutationOutcome.Success)
            await _storage.DeleteIfExistsAsync(stored.Container, stored.StorageKey, ct);
        return MapSubmit(result);
    }

    private async Task<ManualPaymentAccessContext?> AccessAsync(string token, string action, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 40 || token.Length > 200) return null;
        string hash;
        try { hash = _tokens.HashToken(token); }
        catch (InvalidOperationException) { return null; }
        return await _repository.FindAccessAsync(hash, action, _clock.UtcNow, ct);
    }

    private static ApplicationError? ValidateSubmission(ManualPaymentAccessContext context, string method,
        string reference, decimal amount, string currency, DateTimeOffset paymentDate, string? note,
        ManualPaymentEvidenceUpload upload)
    {
        if (!PaymentMethods.Contains(method?.Trim() ?? string.Empty)) return Error("validation_failed", "Payment method is invalid.");
        if (string.IsNullOrWhiteSpace(reference) || reference.Trim().Length is < 4 or > 120)
            return Error("validation_failed", "Bank or transaction reference must contain 4 to 120 characters.");
        if (amount <= 0) return Error("validation_failed", "Submitted amount must be positive.");
        if (amount != context.Payment.ExpectedAmount) return Error("amount_mismatch", "Submitted amount must match the invoice total.");
        if (!string.Equals(currency?.Trim(), context.Payment.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            return Error("currency_mismatch", "Submitted currency must match the invoice currency.");
        if (paymentDate > DateTimeOffset.UtcNow.AddDays(1) || paymentDate < context.Invoice.CreatedAt.AddDays(-30))
            return Error("validation_failed", "Payment date is outside the permitted range.");
        if (note?.Length > 1000) return Error("validation_failed", "Payer note is too long.");
        if (upload.Length <= 0 || upload.Length > MaximumEvidenceBytes)
            return Error("invalid_evidence_size", "Proof must be between 1 byte and 10 MB.");
        if (upload.ContentType?.Trim().ToLowerInvariant() is not ("application/pdf" or "image/jpeg" or "image/png"))
            return Error("invalid_evidence_type", "Proof must be PDF, JPEG, or PNG.");
        if (string.IsNullOrWhiteSpace(upload.OriginalFileName) || upload.OriginalFileName.Length > 255)
            return Error("invalid_evidence_type", "Proof filename is invalid.");
        var extension = Path.GetExtension(upload.OriginalFileName ?? string.Empty).ToLowerInvariant();
        var expectedExtension = upload.ContentType.Trim().ToLowerInvariant() switch
        {
            "application/pdf" => extension == ".pdf",
            "image/png" => extension == ".png",
            "image/jpeg" => extension is ".jpg" or ".jpeg",
            _ => false
        };
        if (!expectedExtension) return Error("invalid_evidence_type", "Proof extension does not match its declared type.");
        return null;
    }

    private static bool HasValidMagic(ReadOnlySpan<byte> bytes, string contentType) => contentType switch
    {
        "application/pdf" => bytes.Length >= 5 && bytes[..5].SequenceEqual("%PDF-"u8),
        "image/png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
        _ => false
    };

    private static string BuildSafeFileName(string original, string contentType)
    {
        var baseName = Path.GetFileNameWithoutExtension(original ?? string.Empty);
        var safe = new string(baseName.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').Take(80).ToArray());
        if (string.IsNullOrWhiteSpace(safe)) safe = "payment-proof";
        var extension = contentType switch { "application/pdf" => ".pdf", "image/png" => ".png", _ => ".jpg" };
        return $"{safe}-{Guid.NewGuid():N}{extension}";
    }

    private static ApplicationResult<ManualPaymentSubmissionResponse> MapSubmit(ManualPaymentSubmitResult result) => result.Outcome switch
    {
        ManualPaymentMutationOutcome.Success or ManualPaymentMutationOutcome.Replay when result.Response is not null =>
            ApplicationResult<ManualPaymentSubmissionResponse>.Success(result.Response),
        ManualPaymentMutationOutcome.NotFound => Failure<ManualPaymentSubmissionResponse>("not_found", "Payment was not found."),
        ManualPaymentMutationOutcome.InvalidAccess => Failure<ManualPaymentSubmissionResponse>("access_invalid_or_expired", "Payment access is invalid or expired."),
        ManualPaymentMutationOutcome.InvalidTransition => Failure<ManualPaymentSubmissionResponse>("invalid_transition", "Payment cannot be submitted in its current state."),
        ManualPaymentMutationOutcome.ConcurrencyConflict => Failure<ManualPaymentSubmissionResponse>("concurrency_conflict", "Payment changed. Reload before trying again."),
        ManualPaymentMutationOutcome.IdempotencyConflict => Failure<ManualPaymentSubmissionResponse>("idempotency_conflict", "Idempotency key was reused with a different request."),
        ManualPaymentMutationOutcome.AmountMismatch => Failure<ManualPaymentSubmissionResponse>("amount_mismatch", "Submitted amount must match the invoice total."),
        ManualPaymentMutationOutcome.CurrencyMismatch => Failure<ManualPaymentSubmissionResponse>("currency_mismatch", "Submitted currency must match the invoice currency."),
        _ => Failure<ManualPaymentSubmissionResponse>("validation_failed", "Payment submission failed validation.")
    };

    private static ApplicationResult<ManualPaymentReviewResponse> MapReview(ManualPaymentReviewResult result) => result.Outcome switch
    {
        ManualPaymentMutationOutcome.Success or ManualPaymentMutationOutcome.Replay when result.Response is not null =>
            ApplicationResult<ManualPaymentReviewResponse>.Success(result.Response),
        ManualPaymentMutationOutcome.NotFound => Failure<ManualPaymentReviewResponse>("not_found", "Manual payment was not found."),
        ManualPaymentMutationOutcome.InvalidTransition => Failure<ManualPaymentReviewResponse>("invalid_transition", "Payment cannot be reviewed in its current state."),
        ManualPaymentMutationOutcome.ConcurrencyConflict => Failure<ManualPaymentReviewResponse>("concurrency_conflict", "Payment changed. Reload before trying again."),
        ManualPaymentMutationOutcome.IdempotencyConflict => Failure<ManualPaymentReviewResponse>("idempotency_conflict", "Idempotency key was reused with a different request."),
        ManualPaymentMutationOutcome.AmountMismatch => Failure<ManualPaymentReviewResponse>("amount_mismatch", "Submitted amount does not match the invoice."),
        ManualPaymentMutationOutcome.ProofRequired => Failure<ManualPaymentReviewResponse>("proof_required", "Payment proof is required."),
        ManualPaymentMutationOutcome.ProofNotClean => Failure<ManualPaymentReviewResponse>("payment_evidence_not_scanned", "Payment proof has not passed malware scanning."),
        _ => Failure<ManualPaymentReviewResponse>("validation_failed", "Payment review failed validation.")
    };

    private Task<bool> CanView(Guid userId, CancellationToken ct) =>
        _permissions.HasPermissionAsync(userId, PlatformPermissionCodes.BillingView, ct);

    private static ApplicationError? ValidateKey(string key) => string.IsNullOrWhiteSpace(key) || key.Trim().Length > 100
        ? Error("precondition_required", "A valid Idempotency-Key is required.") : null;
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static ApplicationError Error(string suffix, string message) => new($"manual_payment.{suffix}", message);
    private static ApplicationResult<T> Failure<T>(string suffix, string message) => ApplicationResult<T>.Failure(Error(suffix, message));
}
