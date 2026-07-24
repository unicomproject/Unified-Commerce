namespace E_POS.Application.Common.Email;

/// <summary>
/// Result of an accepted provider send operation (not inbox delivery confirmation).
/// </summary>
public sealed record ApplicationEmailSendResult(
    string OperationId,
    string Status,
    string? ProviderRequestId = null);
