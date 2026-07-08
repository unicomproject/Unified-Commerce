using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed class PlatformAuditLogService : IPlatformAuditLogService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private static readonly ApplicationError AccessDenied = new(
        "platform_audit.access_denied",
        "Platform audit log access denied.");

    private static readonly ApplicationError InvalidDateRange = new(
        "platform_audit.validation_failed",
        "The audit log date range is invalid.");

    private readonly IPlatformAuditLogRepository _repository;
    private readonly IPlatformPermissionChecker _permissionChecker;

    public PlatformAuditLogService(
        IPlatformAuditLogRepository repository,
        IPlatformPermissionChecker permissionChecker)
    {
        _repository = repository;
        _permissionChecker = permissionChecker;
    }

    public async Task<ApplicationResult<PlatformAuditLogListResponse>> GetAuditLogsAsync(
        PlatformAuditLogListQuery query,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, cancellationToken))
        {
            return ApplicationResult<PlatformAuditLogListResponse>.Failure(AccessDenied);
        }

        ArgumentNullException.ThrowIfNull(query);
        NormalizeQuery(query);

        if (query.From is not null && query.To is not null && query.From > query.To)
        {
            return ApplicationResult<PlatformAuditLogListResponse>.Failure(InvalidDateRange);
        }

        var response = await _repository.GetLoginSecurityAuditLogsAsync(query, cancellationToken);
        return ApplicationResult<PlatformAuditLogListResponse>.Success(response);
    }

    private Task<bool> HasPermissionAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        return _permissionChecker.HasPermissionAsync(
            platformUserId,
            PlatformPermissionCodes.AuditView,
            cancellationToken);
    }

    private static void NormalizeQuery(PlatformAuditLogListQuery query)
    {
        query.PageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        query.PageSize = query.PageSize < 1 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);
        query.Search = NormalizeOptionalText(query.Search);
        query.Action = NormalizeOptionalText(query.Action);
        query.EntityType = NormalizeOptionalText(query.EntityType);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}


