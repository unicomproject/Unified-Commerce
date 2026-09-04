namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public sealed record TenantAdminInvitationDeliveryRequest(
    Guid TenantId,
    string TenantName,
    string TenantCode,
    string AdminEmail,
    string? AdminDisplayName,
    string RawToken,
    DateTimeOffset ExpiresAt,
    string? CorrelationId = null);

public sealed record TenantAdminInvitationDeliveryResult(
    bool IsSuccess,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public interface ITenantAdminInvitationDeliveryService
{
    Task<TenantAdminInvitationDeliveryResult> DeliverAsync(
        TenantAdminInvitationDeliveryRequest request,
        CancellationToken cancellationToken);
}
