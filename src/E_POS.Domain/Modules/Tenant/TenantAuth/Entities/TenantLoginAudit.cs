using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

public class TenantLoginAudit : AuditableEntity
{
    public Guid? TenantId { get; protected set; }
    public Guid? UserId { get; protected set; }
    public Guid? AuthSessionId { get; protected set; }
    public Guid? PosDeviceId { get; protected set; }
    public string AttemptedIdentifier { get; protected set; } = string.Empty;
    public string AuthenticationMethod { get; protected set; } = string.Empty;
    public string LoginStatus { get; protected set; } = string.Empty;
    public string? FailureCode { get; protected set; }
    public string? FailureDetail { get; protected set; }
    public string? IpAddress { get; protected set; }
    public string? UserAgent { get; protected set; }
    public DateTimeOffset AttemptedAt { get; protected set; }

    public static TenantLoginAudit Create(
        Guid id,
        Guid? tenantId,
        Guid? userId,
        Guid? authSessionId,
        Guid? posDeviceId,
        string attemptedIdentifier,
        string authenticationMethod,
        string loginStatus,
        string? failureCode,
        string? failureDetail,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset now)
    {
        return new TenantLoginAudit
        {
            Id = id,
            TenantId = tenantId,
            UserId = userId,
            AuthSessionId = authSessionId,
            PosDeviceId = posDeviceId,
            AttemptedIdentifier = attemptedIdentifier,
            AuthenticationMethod = authenticationMethod,
            LoginStatus = loginStatus,
            FailureCode = failureCode,
            FailureDetail = failureDetail,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            AttemptedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
