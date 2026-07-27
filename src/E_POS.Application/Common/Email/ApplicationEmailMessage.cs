namespace E_POS.Application.Common.Email;

/// <summary>
/// Provider-neutral email message. Does not expose Azure SDK types.
/// </summary>
public sealed record ApplicationEmailMessage(
    string ToAddress,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null,
    string? CorrelationId = null);
