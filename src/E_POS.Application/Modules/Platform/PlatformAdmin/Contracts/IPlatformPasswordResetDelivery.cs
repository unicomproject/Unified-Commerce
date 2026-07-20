namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformPasswordResetLinkBuilder
{
    string BuildResetUrl(string rawToken);
}

public interface IPlatformPasswordResetDeliveryService
{
    Task<PlatformPasswordResetDeliveryResult> DeliverAsync(
        PlatformPasswordResetDeliveryRequest request,
        CancellationToken cancellationToken);
}

public sealed record PlatformPasswordResetDeliveryRequest(
    Guid PlatformUserId,
    string Email,
    string RawToken,
    string ResetUrl,
    DateTimeOffset ExpiresAt);

public sealed record PlatformPasswordResetDeliveryResult(
    string DeliveryMode,
    string? ResetUrlForAdmin,
    string Message);
