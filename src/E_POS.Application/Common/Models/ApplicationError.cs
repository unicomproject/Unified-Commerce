namespace E_POS.Application.Common.Models;

public sealed record ApplicationError(
    string Code,
    string Message,
    IReadOnlyList<ApplicationFieldError>? FieldErrors = null)
{
    public static ApplicationError None { get; } = new(string.Empty, string.Empty);

    public static ApplicationError ValidationFailed(
        string message,
        IReadOnlyList<ApplicationFieldError> fieldErrors) =>
        new("platform_tenants.validation_failed", message, fieldErrors);
}