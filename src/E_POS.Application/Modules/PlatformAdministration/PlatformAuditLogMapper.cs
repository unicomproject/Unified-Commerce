using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.PlatformAdministration.Constants;

namespace E_POS.Application.Modules.PlatformAdministration;

public static class PlatformAuditLogMapper
{
    public const string AuditScope = "platform_login_security";
    public const string AuditScopeDescription =
        "Platform login and authentication security events from platform_login_audits. Generic business audit logs are not available in Release 1.";
    public const string Area = "platform_auth";
    public const string EntityType = "platform_user";

    public static PlatformAuditLogListItemDto MapLoginAudit(
        Guid id,
        DateTimeOffset occurredAt,
        Guid? platformUserId,
        string? email,
        string loginResult)
    {
        var action = MapAction(loginResult);
        return new PlatformAuditLogListItemDto(
            id,
            occurredAt,
            new PlatformAuditLogActorDto(platformUserId, email),
            action,
            Area,
            EntityType,
            platformUserId,
            MapSummary(loginResult),
            IpAddress: null,
            UserAgent: null);
    }

    public static string MapAction(string loginResult)
    {
        return loginResult switch
        {
            _ when string.Equals(loginResult, PlatformAuthConstants.SuccessLoginResult, StringComparison.Ordinal) =>
                "platform.login.success",
            _ when string.Equals(loginResult, PlatformAuthConstants.FailedLoginResult, StringComparison.Ordinal) =>
                "platform.login.failed",
            _ when string.Equals(loginResult, PlatformAuthConstants.LockedLoginResult, StringComparison.Ordinal) =>
                "platform.login.locked",
            _ => "platform.login.unknown"
        };
    }

    public static string MapSummary(string loginResult)
    {
        return loginResult switch
        {
            _ when string.Equals(loginResult, PlatformAuthConstants.SuccessLoginResult, StringComparison.Ordinal) =>
                "Platform login succeeded.",
            _ when string.Equals(loginResult, PlatformAuthConstants.FailedLoginResult, StringComparison.Ordinal) =>
                "Platform login failed.",
            _ when string.Equals(loginResult, PlatformAuthConstants.LockedLoginResult, StringComparison.Ordinal) =>
                "Platform login blocked because the account is locked.",
            _ => "Platform login event recorded."
        };
    }

    public static string? ResolveLoginResultFilter(string? action)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return null;
        }

        var normalized = action.Trim();

        return normalized switch
        {
            "platform.login.success" or "SUCCESS" => PlatformAuthConstants.SuccessLoginResult,
            "platform.login.failed" or "FAILED" => PlatformAuthConstants.FailedLoginResult,
            "platform.login.locked" or "LOCKED" => PlatformAuthConstants.LockedLoginResult,
            _ => normalized.ToUpperInvariant()
        };
    }
}
