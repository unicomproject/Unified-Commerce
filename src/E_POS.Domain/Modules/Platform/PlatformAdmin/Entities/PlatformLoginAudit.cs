using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformLoginAudit : AuditableEntity
{
    public Guid? PlatformUserId { get; protected set; }
    public string LoginResult { get; protected set; } = string.Empty;
    public Guid? PlatformAuthSessionId { get; protected set; }
    public string? AuthenticationMethod { get; protected set; }
    public DateTimeOffset? AttemptedAt { get; protected set; }
    public string? IpAddress { get; protected set; }
    public string? UserAgent { get; protected set; }
    public string? LoginStatus { get; protected set; }
    public string? FailureReason { get; protected set; }
    public int? RiskScore { get; protected set; }

    public static PlatformLoginAudit Create(
        Guid id,
        Guid? platformUserId,
        string loginStatus,
        DateTimeOffset now,
        string? authenticationMethod = null,
        DateTimeOffset? attemptedAt = null,
        Guid? platformAuthSessionId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? failureReason = null)
    {
        if (string.IsNullOrWhiteSpace(loginStatus))
        {
            throw new ArgumentException("Login status is required.", nameof(loginStatus));
        }

        return new PlatformLoginAudit
        {
            Id = id,
            PlatformUserId = platformUserId,
            LoginStatus = loginStatus,
            // Compatibility write retained until legacy login_result retirement migration.
            LoginResult = loginStatus,
            AttemptedAt = attemptedAt ?? now,
            AuthenticationMethod = string.IsNullOrWhiteSpace(authenticationMethod)
                ? PlatformAuthAlignmentConstants.AuthenticationMethod.Password
                : authenticationMethod,
            PlatformAuthSessionId = platformAuthSessionId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            FailureReason = failureReason,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Represents a pre-8B row shape (login_result only, no Second Brain columns) for migration backfill testing.
    /// </summary>
    public static PlatformLoginAudit CreateLegacy(
        Guid id,
        Guid? platformUserId,
        string loginResult,
        DateTimeOffset createdAt)
    {
        return new PlatformLoginAudit
        {
            Id = id,
            PlatformUserId = platformUserId,
            LoginResult = loginResult,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    /// <summary>
    /// Applies Phase 8A backfill for Second Brain login-audit columns without inventing client metadata.
    /// </summary>
    public void ApplyAlignmentBackfill()
    {
        if (AttemptedAt is null)
        {
            AttemptedAt = CreatedAt;
        }

        if (string.IsNullOrWhiteSpace(LoginStatus) && !string.IsNullOrWhiteSpace(LoginResult))
        {
            LoginStatus = LoginResult;
        }

        if (string.IsNullOrWhiteSpace(AuthenticationMethod))
        {
            AuthenticationMethod = PlatformAuthAlignmentConstants.AuthenticationMethod.Password;
        }
    }
}
