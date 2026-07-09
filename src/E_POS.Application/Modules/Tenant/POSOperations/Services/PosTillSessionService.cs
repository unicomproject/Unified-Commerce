using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Application.Modules.Tenant.POSOperations.Services;

public sealed class PosTillSessionService : IPosTillSessionService
{
    private static readonly ApplicationError InvalidDeviceId = new(
        "till_session.invalid_device_id",
        "Device id is required.");

    private static readonly ApplicationError InvalidTillId = new(
        "till_session.invalid_till_id",
        "Till id is required.");

    private static readonly ApplicationError InvalidOpeningFloat = new(
        "till_session.invalid_opening_float",
        "Opening float must be zero or greater.");

    private static readonly ApplicationError ViewPermissionDenied = new(
        "till_session.permission_denied",
        "You do not have permission to view till sessions.");

    private static readonly ApplicationError OpenPermissionDenied = new(
        "till_session.permission_denied",
        "You do not have permission to open a till.");

    private static readonly ApplicationError DeviceNotFound = new(
        "till_session.device_not_found",
        "POS device could not be found.");

    private static readonly ApplicationError DeviceNotTrusted = new(
        "till_session.device_not_trusted",
        "This POS device is not trusted.");

    private static readonly ApplicationError TillNotAssigned = new(
        "till_session.till_not_assigned",
        "No till is assigned to this POS device.");

    private static readonly ApplicationError TillMismatch = new(
        "till_session.till_mismatch",
        "The requested till does not match this POS device.");

    private static readonly ApplicationError TillNotFound = new(
        "till_session.till_not_found",
        "Till could not be found.");

    private static readonly ApplicationError TillInactive = new(
        "till_session.till_inactive",
        "Till is not active.");

    private static readonly ApplicationError AlreadyOpen = new(
        "till_session.already_open",
        "An open till session already exists for this till.");

    private static readonly ApplicationError NotFound = new(
        "till_session.not_found",
        "No open till session was found for this device.");

    private static readonly ApplicationError ClosePermissionDenied = new(
        "till_session.permission_denied",
        "You do not have permission to close a till.");

    private static readonly ApplicationError InvalidCountedCash = new(
        "till_session.invalid_counted_cash",
        "Counted cash must be zero or greater.");

    private static readonly ApplicationError InvalidExpectedCash = new(
        "till_session.invalid_expected_cash",
        "Expected cash must be zero or greater.");

    private static readonly ApplicationError NotOpen = new(
        "till_session.not_open",
        "No open till session was found to close.");

    private static readonly ApplicationError MismatchReasonRequired = new(
        "till_session.mismatch_reason_required",
        "A mismatch reason is required when counted cash does not match expected cash.");

    private readonly IPosTillSessionRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PosTillSessionService(
        IPosTillSessionRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<CurrentTillSessionResponseDto>> GetCurrentSessionAsync(
        TenantRequestContext context,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ApplicationResult<CurrentTillSessionResponseDto>.Failure(InvalidDeviceId);
        }

        var canViewSession =
            context.HasPermission(PosPermissions.Till.Open) ||
            context.HasPermission(PosPermissions.Till.Close) ||
            context.HasPermission(PosPermissions.Till.ViewSession);

        if (!canViewSession)
        {
            return ApplicationResult<CurrentTillSessionResponseDto>.Failure(ViewPermissionDenied);
        }

        var result = await _repository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);

        if (!result.IsSuccess || result.Snapshot is null)
        {
            return ApplicationResult<CurrentTillSessionResponseDto>.Failure(
                MapResolveError(result.ErrorCode));
        }

        return ApplicationResult<CurrentTillSessionResponseDto>.Success(MapToResponse(result.Snapshot));
    }

    public async Task<ApplicationResult<CurrentTillSessionResponseDto>> OpenTillAsync(
        TenantRequestContext context,
        OpenTillRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DeviceId == Guid.Empty)
        {
            return ApplicationResult<CurrentTillSessionResponseDto>.Failure(InvalidDeviceId);
        }

        if (request.TillId == Guid.Empty)
        {
            return ApplicationResult<CurrentTillSessionResponseDto>.Failure(InvalidTillId);
        }

        if (request.OpeningFloat < 0)
        {
            return ApplicationResult<CurrentTillSessionResponseDto>.Failure(InvalidOpeningFloat);
        }

        if (!context.HasPermission(PosPermissions.Till.Open))
        {
            return ApplicationResult<CurrentTillSessionResponseDto>.Failure(OpenPermissionDenied);
        }

        var result = await _repository.OpenTillAsync(
            context.TenantId,
            context.UserId,
            new OpenTillCommand(
                request.DeviceId,
                request.TillId,
                request.OpeningFloat,
                string.IsNullOrWhiteSpace(request.OpeningNote) ? null : request.OpeningNote.Trim()),
            _dateTimeProvider.UtcNow,
            cancellationToken);

        if (!result.IsSuccess || result.Snapshot is null)
        {
            return ApplicationResult<CurrentTillSessionResponseDto>.Failure(
                MapOpenError(result.ErrorCode));
        }

        return ApplicationResult<CurrentTillSessionResponseDto>.Success(MapToResponse(result.Snapshot));
    }

    public async Task<ApplicationResult<CloseTillResponseDto>> CloseTillAsync(
        TenantRequestContext context,
        CloseTillRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DeviceId == Guid.Empty)
        {
            return ApplicationResult<CloseTillResponseDto>.Failure(InvalidDeviceId);
        }

        if (request.TillId == Guid.Empty)
        {
            return ApplicationResult<CloseTillResponseDto>.Failure(InvalidTillId);
        }

        if (request.CountedCash < 0)
        {
            return ApplicationResult<CloseTillResponseDto>.Failure(InvalidCountedCash);
        }

        if (request.ExpectedCash is < 0)
        {
            return ApplicationResult<CloseTillResponseDto>.Failure(InvalidExpectedCash);
        }

        if (!context.HasPermission(PosPermissions.Till.Close))
        {
            return ApplicationResult<CloseTillResponseDto>.Failure(ClosePermissionDenied);
        }

        var result = await _repository.CloseTillAsync(
            context.TenantId,
            context.UserId,
            new CloseTillCommand(
                request.DeviceId,
                request.TillId,
                request.CountedCash,
                request.ExpectedCash,
                string.IsNullOrWhiteSpace(request.MismatchReason) ? null : request.MismatchReason.Trim(),
                string.IsNullOrWhiteSpace(request.ClosingNote) ? null : request.ClosingNote.Trim()),
            _dateTimeProvider.UtcNow,
            cancellationToken);

        if (!result.IsSuccess || result.Snapshot is null)
        {
            return ApplicationResult<CloseTillResponseDto>.Failure(
                MapCloseError(result.ErrorCode));
        }

        return ApplicationResult<CloseTillResponseDto>.Success(MapCloseToResponse(result.Snapshot));
    }

    private static ApplicationError MapResolveError(string? errorCode) =>
        errorCode switch
        {
            "till_session.device_not_found" => DeviceNotFound,
            "till_session.device_not_trusted" => DeviceNotTrusted,
            "till_session.till_not_assigned" => TillNotAssigned,
            _ => NotFound,
        };

    private static ApplicationError MapOpenError(string? errorCode) =>
        errorCode switch
        {
            "till_session.device_not_found" => DeviceNotFound,
            "till_session.device_not_trusted" => DeviceNotTrusted,
            "till_session.till_not_assigned" => TillNotAssigned,
            "till_session.till_mismatch" => TillMismatch,
            "till_session.till_not_found" => TillNotFound,
            "till_session.till_inactive" => TillInactive,
            "till_session.already_open" => AlreadyOpen,
            _ => new ApplicationError(
                errorCode ?? "till_session.open_failed",
                "Till could not be opened."),
        };

    private static ApplicationError MapCloseError(string? errorCode) =>
        errorCode switch
        {
            "till_session.device_not_found" => DeviceNotFound,
            "till_session.device_not_trusted" => DeviceNotTrusted,
            "till_session.till_not_assigned" => TillNotAssigned,
            "till_session.till_mismatch" => TillMismatch,
            "till_session.not_open" => NotOpen,
            "till_session.invalid_counted_cash" => InvalidCountedCash,
            "till_session.invalid_expected_cash" => InvalidExpectedCash,
            "till_session.mismatch_reason_required" => MismatchReasonRequired,
            _ => new ApplicationError(
                errorCode ?? "till_session.close_failed",
                "Till could not be closed."),
        };

    private static CurrentTillSessionResponseDto MapToResponse(CurrentTillSessionDbSnapshot snapshot) =>
        new(new CurrentTillSessionDto(
            Id: snapshot.SessionId,
            OutletId: snapshot.OutletId,
            TillId: snapshot.TillId,
            OpenedDeviceId: snapshot.OpenedDeviceId,
            OpeningFloat: snapshot.OpeningFloat,
            Status: NormalizeStatus(snapshot.Status),
            OpenedAt: snapshot.OpenedAt,
            OpeningNote: snapshot.OpeningNote));

    private static CloseTillResponseDto MapCloseToResponse(ClosedTillSessionDbSnapshot snapshot) =>
        new(new ClosedTillSessionDto(
            Id: snapshot.SessionId,
            OutletId: snapshot.OutletId,
            TillId: snapshot.TillId,
            OpeningFloat: snapshot.OpeningFloat,
            ExpectedCash: snapshot.ExpectedCash,
            CountedCash: snapshot.CountedCash,
            CashDifference: snapshot.CashDifference,
            Status: NormalizeStatus(snapshot.Status),
            OpenedAt: snapshot.OpenedAt,
            ClosedAt: snapshot.ClosedAt,
            ClosingNote: snapshot.ClosingNote));

    private static string NormalizeStatus(string status) =>
        string.Equals(status, "OPEN", StringComparison.OrdinalIgnoreCase)
            ? "open"
            : string.Equals(status, "CLOSED", StringComparison.OrdinalIgnoreCase)
                ? "closed"
                : status.Trim().ToLowerInvariant();
}
