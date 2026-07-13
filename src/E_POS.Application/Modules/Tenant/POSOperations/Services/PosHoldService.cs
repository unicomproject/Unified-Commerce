using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.Orders.Constants;

namespace E_POS.Application.Modules.Tenant.POSOperations.Services;

public sealed class PosHoldService : IPosHoldService
{
    private static readonly ApplicationError PermissionDenied = new(
        "pos_holds.permission_denied",
        "You do not have permission to view parked POS sales.");

    private readonly IPosHoldRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PosHoldService(IPosHoldRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<bool>> CancelHoldAsync(
        TenantRequestContext context,
        Guid holdId,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(SalesPermissions.Park.Create))
            return CancelFailure("pos_holds.permission_denied",
                "You do not have permission to cancel parked POS sales.");
        if (holdId == Guid.Empty)
            return CancelFailure("pos_holds.invalid_hold_id", "Hold id is required.");
        if (reason?.Trim().Length > 250)
            return CancelFailure("pos_holds.invalid_reason",
                "Cancellation reason cannot exceed 250 characters.");

        var result = await _repository.CancelHoldAsync(
            context.TenantId, context.UserId, holdId, reason,
            _dateTimeProvider.UtcNow, cancellationToken);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode ?? "pos_holds.cancel_failed";
            return CancelFailure(code, code switch
            {
                "pos_holds.not_found" => "Parked sale could not be found.",
                "pos_holds.expired" => "The parked sale has expired.",
                "pos_holds.not_cancellable" =>
                    "The parked sale has already been recalled or cancelled.",
                _ => "Parked sale could not be cancelled."
            });
        }

        return ApplicationResult<bool>.Success(true);
    }

    public async Task<ApplicationResult<PosRecallHoldResponseDto>> RecallHoldAsync(
        TenantRequestContext context,
        Guid holdId,
        PosRecallHoldRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(SalesPermissions.Park.Recall))
            return RecallFailure("pos_holds.permission_denied",
                "You do not have permission to recall parked POS sales.");
        if (holdId == Guid.Empty)
            return RecallFailure("pos_holds.invalid_hold_id", "Hold id is required.");
        if (request.DeviceId == Guid.Empty)
            return RecallFailure("pos_holds.invalid_device_id", "Device id is required.");

        var result = await _repository.RecallHoldAsync(
            context.TenantId, context.UserId, context.Permissions, holdId,
            request, _dateTimeProvider.UtcNow, cancellationToken);
        if (!result.IsSuccess || result.Recall is null)
        {
            var code = result.ErrorCode ?? "pos_holds.recall_failed";
            return RecallFailure(code, code switch
            {
                "pos_holds.not_found" => "Parked sale could not be found.",
                "pos_holds.expired" => "The parked sale has expired.",
                "pos_holds.not_recallable" => "The parked sale has already been recalled or cancelled.",
                "pos_holds.till_mismatch" => "The parked sale belongs to a different till.",
                "pos_checkout.till_session_not_open" or "till_session.not_found" =>
                    "An open till session is required before recalling a sale.",
                "pos_checkout.insufficient_stock" => "Stock is no longer sufficient for this parked sale.",
                "pos_checkout.price_not_configured" => "Current pricing is unavailable for this parked sale.",
                _ => "Parked sale could not be recalled."
            });
        }

        return ApplicationResult<PosRecallHoldResponseDto>.Success(result.Recall);
    }

    public async Task<ApplicationResult<PosHoldListItemDto>> CreateHoldAsync(
        TenantRequestContext context,
        PosCreateHoldRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(SalesPermissions.Park.Create))
        {
            return ApplicationResult<PosHoldListItemDto>.Failure(new ApplicationError(
                "pos_holds.permission_denied",
                "You do not have permission to park POS sales."));
        }

        if (request.DeviceId == Guid.Empty)
            return Failure("pos_holds.invalid_device_id", "Device id is required.");
        if (request.Lines is null || request.Lines.Count == 0 ||
            request.Lines.Any(x => x.VariantId == Guid.Empty || x.Qty <= 0))
            return Failure("pos_holds.invalid_lines", "A hold requires at least one valid cart line.");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Trim().Length > 100)
            return Failure("pos_holds.invalid_idempotency_key",
                "A valid idempotency key of at most 100 characters is required.");
        if (request.Reason?.Trim().Length > 250)
            return Failure("pos_holds.invalid_reason", "Hold reason cannot exceed 250 characters.");
        if (request.ExpiresAt.HasValue && request.ExpiresAt <= _dateTimeProvider.UtcNow)
            return Failure("pos_holds.invalid_expiry", "Hold expiry must be in the future.");

        var result = await _repository.CreateHoldAsync(
            context.TenantId, context.UserId, context.Permissions, request,
            _dateTimeProvider.UtcNow, cancellationToken);
        if (!result.IsSuccess || result.Hold is null)
        {
            var code = result.ErrorCode ?? "pos_holds.create_failed";
            return Failure(code, code switch
            {
                "pos_holds.idempotency_conflict" => "The idempotency key was used for another hold.",
                "pos_checkout.device_not_found" => "POS device could not be found.",
                "pos_checkout.till_session_not_open" or "till_session.not_found" =>
                    "An open till session is required before parking a sale.",
                "pos_checkout.variant_not_found" => "One or more product variants could not be found.",
                "pos_checkout.customer_not_found" => "The selected customer could not be found.",
                "pos_checkout.insufficient_stock" => "One or more cart lines do not have enough stock.",
                "pos_checkout.price_not_configured" => "One or more cart lines do not have a configured price.",
                _ => "POS sale could not be parked."
            });
        }

        return ApplicationResult<PosHoldListItemDto>.Success(result.Hold);
    }

    public async Task<ApplicationResult<PosHoldListResponseDto>> GetHoldsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(SalesPermissions.Park.View))
        {
            return ApplicationResult<PosHoldListResponseDto>.Failure(PermissionDenied);
        }

        var holds = await _repository.GetActiveHoldsAsync(
            context.TenantId,
            context.UserId,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return ApplicationResult<PosHoldListResponseDto>.Success(
            new PosHoldListResponseDto(holds, holds.Count));
    }

    private static ApplicationResult<PosHoldListItemDto> Failure(string code, string message) =>
        ApplicationResult<PosHoldListItemDto>.Failure(new ApplicationError(code, message));

    private static ApplicationResult<PosRecallHoldResponseDto> RecallFailure(string code, string message) =>
        ApplicationResult<PosRecallHoldResponseDto>.Failure(new ApplicationError(code, message));

    private static ApplicationResult<bool> CancelFailure(string code, string message) =>
        ApplicationResult<bool>.Failure(new ApplicationError(code, message));
}
