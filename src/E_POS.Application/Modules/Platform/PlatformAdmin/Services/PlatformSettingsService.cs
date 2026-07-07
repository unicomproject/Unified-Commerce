using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Validators;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed class PlatformSettingsService : IPlatformSettingsService
{
    private static readonly ApplicationError AccessDenied = new(
        "platform_settings.access_denied",
        "Platform settings access denied.");

    private readonly IPlatformSettingsRepository _repository;
    private readonly IPlatformPermissionChecker _permissionChecker;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PlatformSettingsService(
        IPlatformSettingsRepository repository,
        IPlatformPermissionChecker permissionChecker,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _permissionChecker = permissionChecker;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<PlatformSettingsResponse>> GetSettingsAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasViewPermissionAsync(platformUserId, cancellationToken))
        {
            return ApplicationResult<PlatformSettingsResponse>.Failure(AccessDenied);
        }

        var settings = await _repository.GetGeneralSettingsAsync(cancellationToken);
        return ApplicationResult<PlatformSettingsResponse>.Success(settings);
    }

    public async Task<ApplicationResult<PlatformSettingsResponse>> UpdateSettingsAsync(
        UpdatePlatformSettingsRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasUpdatePermissionAsync(platformUserId, cancellationToken))
        {
            return ApplicationResult<PlatformSettingsResponse>.Failure(AccessDenied);
        }

        var validationError = PlatformSettingsRequestValidator.ValidateUpdate(request);
        if (validationError is not null)
        {
            return ApplicationResult<PlatformSettingsResponse>.Failure(validationError);
        }

        var settings = await _repository.SaveGeneralSettingsAsync(
            request,
            platformUserId,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return ApplicationResult<PlatformSettingsResponse>.Success(settings);
    }

    private Task<bool> HasViewPermissionAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        return _permissionChecker.HasPermissionAsync(
            platformUserId,
            PlatformPermissionCodes.SettingsView,
            cancellationToken);
    }

    private Task<bool> HasUpdatePermissionAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        return _permissionChecker.HasPermissionAsync(
            platformUserId,
            PlatformPermissionCodes.SettingsUpdate,
            cancellationToken);
    }
}


