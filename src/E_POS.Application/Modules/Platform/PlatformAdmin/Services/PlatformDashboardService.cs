using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed class PlatformDashboardService : IPlatformDashboardService
{
    private static readonly ApplicationError AccessDenied = new(
        "platform_dashboard.access_denied",
        "Platform dashboard access denied.");

    private readonly IPlatformDashboardRepository _repository;
    private readonly IPlatformPermissionChecker _permissionChecker;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PlatformDashboardService(
        IPlatformDashboardRepository repository,
        IPlatformPermissionChecker permissionChecker,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _permissionChecker = permissionChecker;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<PlatformDashboardResponse>> GetDashboardAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await _permissionChecker.HasPermissionAsync(
                platformUserId,
                PlatformPermissionCodes.DashboardView,
                cancellationToken))
        {
            return ApplicationResult<PlatformDashboardResponse>.Failure(AccessDenied);
        }

        var dashboard = await _repository.GetDashboardAsync(_dateTimeProvider.UtcNow, cancellationToken);
        return ApplicationResult<PlatformDashboardResponse>.Success(dashboard);
    }
}


