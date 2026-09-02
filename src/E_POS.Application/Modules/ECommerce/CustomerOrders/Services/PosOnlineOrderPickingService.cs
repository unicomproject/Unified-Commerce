using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Constants;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Services;

public sealed class PosOnlineOrderPickingService : IPosOnlineOrderPickingService
{
    public const string AccessPermission = OnlineOrderPickingPermissions.OrdersAccess;
    public const string ViewPermission = OnlineOrderPickingPermissions.PickingView;
    public const string PickPermission = OnlineOrderPickingPermissions.PickingPick;
    public const string ScanPermission = OnlineOrderPickingPermissions.PickingScan;
    public const string ManualEntryPermission = OnlineOrderPickingPermissions.PickingManualEntry;
    public const string ReportIssuePermission = OnlineOrderPickingPermissions.PickingReportIssue;
    public const string NotePermission = OnlineOrderPickingPermissions.PickingNote;
    public const int PickingNoteMaxLength = 500;

    private readonly IPosOnlineOrderPickingRepository _repository;
    private readonly ITenantFeatureEntitlementEvaluator _entitlements;
    private readonly IDateTimeProvider _clock;

    public PosOnlineOrderPickingService(
        IPosOnlineOrderPickingRepository repository,
        ITenantFeatureEntitlementEvaluator entitlements,
        IDateTimeProvider clock)
    {
        _repository = repository;
        _entitlements = entitlements;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosOnlineOrderPickingResponse>> GetAsync(
        TenantRequestContext context, Guid outletId, Guid orderId,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateBaseAsync(context, outletId, ViewPermission, cancellationToken);
        if (accessError is not null)
            return QueryFailure(accessError);
        if (orderId == Guid.Empty)
            return QueryFailure(new("online_orders.invalid_order_id", "A valid order id is required."));

        var result = await _repository.GetAsync(
            context.TenantId, context.UserId, outletId, orderId, _clock.UtcNow, cancellationToken);
        return result.IsSuccess && result.Picking is not null
            ? ApplicationResult<PosOnlineOrderPickingResponse>.Success(result.Picking)
            : QueryFailure(MapRepositoryError(result.ErrorCode));
    }

    public async Task<ApplicationResult<PosOnlineOrderPickingCommandResponse>> PickLineAsync(
        TenantRequestContext context, Guid outletId, Guid orderId, Guid lineId,
        PosOnlineOrderPickLineRequest request, CancellationToken cancellationToken)
    {
        var inputMethod = request.InputMethod.Trim().ToUpperInvariant();
        var inputPermission = inputMethod switch
        {
            "SCAN" => ScanPermission,
            "MANUAL" => ManualEntryPermission,
            _ => string.Empty
        };
        if (inputPermission.Length == 0)
            return CommandFailure(new("online_orders.invalid_input_method", "Input method must be SCAN or MANUAL."));

        var accessError = await ValidateBaseAsync(context, outletId, PickPermission, cancellationToken);
        if (accessError is not null)
            return CommandFailure(accessError);
        if (!context.HasPermission(inputPermission))
            return CommandFailure(new("online_orders.permission_denied", "Permission denied for this picking input method."));
        if (orderId == Guid.Empty || lineId == Guid.Empty)
            return CommandFailure(new("online_orders.invalid_line", "A valid order and fulfilment line are required."));
        if (request.Quantity <= 0)
            return CommandFailure(new("online_orders.invalid_quantity", "Pick quantity must be greater than zero."));
        if (request.ExpectedVersion <= 0)
            return CommandFailure(new("online_orders.invalid_expected_version", "A positive expectedVersion is required."));
        if (inputMethod == "SCAN" && string.IsNullOrWhiteSpace(request.Barcode))
            return CommandFailure(new("online_orders.invalid_barcode", "A barcode is required for scanned picking."));

        var normalized = new PosOnlineOrderPickLineRequest
        {
            Quantity = request.Quantity,
            Barcode = request.Barcode?.Trim(),
            InputMethod = inputMethod,
            ExpectedVersion = request.ExpectedVersion
        };
        var result = await _repository.PickLineAsync(
            context.TenantId, context.UserId, outletId, orderId, lineId,
            normalized, _clock.UtcNow, cancellationToken);
        return result.IsSuccess && result.Command is not null
            ? ApplicationResult<PosOnlineOrderPickingCommandResponse>.Success(result.Command)
            : CommandFailure(MapRepositoryError(result.ErrorCode));
    }

    public async Task<ApplicationResult<PosOnlineOrderPickingCommandResponse>> ReportIssueAsync(
        TenantRequestContext context, Guid outletId, Guid orderId, Guid lineId,
        PosOnlineOrderPickingIssueRequest request, CancellationToken cancellationToken)
    {
        var accessError = await ValidateBaseAsync(context, outletId, ReportIssuePermission, cancellationToken);
        if (accessError is not null)
            return CommandFailure(accessError);
        if (orderId == Guid.Empty || lineId == Guid.Empty)
            return CommandFailure(new("online_orders.invalid_line", "A valid order and fulfilment line are required."));
        if (request.ExpectedVersion <= 0)
            return CommandFailure(new("online_orders.invalid_expected_version", "A positive expectedVersion is required."));
        if (!string.Equals(request.Reason?.Trim(), "ITEM_NOT_FOUND", StringComparison.OrdinalIgnoreCase))
            return CommandFailure(new("online_orders.invalid_issue", "The supported picking issue reason is ITEM_NOT_FOUND."));
        if (request.Note?.Trim().Length > 500)
            return CommandFailure(new("online_orders.invalid_issue", "Issue note must not exceed 500 characters."));

        var normalized = new PosOnlineOrderPickingIssueRequest
        {
            Reason = "ITEM_NOT_FOUND",
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            ExpectedVersion = request.ExpectedVersion
        };
        var result = await _repository.ReportIssueAsync(
            context.TenantId, context.UserId, outletId, orderId, lineId,
            normalized, _clock.UtcNow, cancellationToken);
        return result.IsSuccess && result.Command is not null
            ? ApplicationResult<PosOnlineOrderPickingCommandResponse>.Success(result.Command)
            : CommandFailure(MapRepositoryError(result.ErrorCode));
    }

    public async Task<ApplicationResult<PosOnlineOrderPickingNoteCommandResponse>> AddNoteAsync(
        TenantRequestContext context, Guid outletId, Guid orderId,
        PosOnlineOrderPickingNoteRequest request, CancellationToken cancellationToken)
    {
        var accessError = await ValidateBaseAsync(context, outletId, NotePermission, cancellationToken);
        if (accessError is not null)
            return NoteFailure(accessError);
        if (orderId == Guid.Empty)
            return NoteFailure(new("online_orders.invalid_order_id", "A valid order id is required."));
        if (request.ExpectedVersion <= 0)
            return NoteFailure(new("online_orders.invalid_expected_version", "A positive expectedVersion is required."));

        var note = request.Note?.Trim();
        if (string.IsNullOrWhiteSpace(note))
            return NoteFailure(new("online_orders.invalid_note", "Picking note is required."));
        if (note.Length > PickingNoteMaxLength)
            return NoteFailure(new("online_orders.invalid_note", $"Picking note must not exceed {PickingNoteMaxLength} characters."));

        var result = await _repository.AddNoteAsync(
            context.TenantId, context.UserId, outletId, orderId,
            new PosOnlineOrderPickingNoteRequest
            {
                Note = note,
                ExpectedVersion = request.ExpectedVersion
            },
            _clock.UtcNow, cancellationToken);
        return result.IsSuccess && result.NoteCommand is not null
            ? ApplicationResult<PosOnlineOrderPickingNoteCommandResponse>.Success(result.NoteCommand)
            : NoteFailure(MapRepositoryError(result.ErrorCode));
    }

    private async Task<ApplicationError?> ValidateBaseAsync(
        TenantRequestContext context, Guid outletId, string operationPermission,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
            return new("online_orders.invalid_tenant_context", "Invalid tenant context.");
        if (!context.HasPermission(AccessPermission) || !context.HasPermission(operationPermission))
            return new("online_orders.permission_denied", "Permission denied for online-order picking.");
        if (outletId == Guid.Empty)
            return new("online_orders.invalid_outlet", "A valid outlet id is required.");

        var entitlement = await _entitlements.EvaluateAsync(
            context.TenantId, PlatformTenantFeatureCodes.ClickCollect,
            _clock.UtcNow, cancellationToken);
        return entitlement.IsAllowed
            ? null
            : new("online_orders.feature_not_entitled", "Click & collect is not enabled for this tenant.");
    }

    private static ApplicationError MapRepositoryError(string? code) => code switch
    {
        "online_orders.outlet_access_denied" => new(code, "You do not have access to this outlet."),
        "online_orders.not_found" => new(code, "Online order was not found."),
        "online_orders.invalid_state" => new(code, "The order is not available for picking."),
        "online_orders.concurrency_conflict" => new(code, "The order changed. Refresh before trying again."),
        "online_orders.invalid_line" => new(code, "The fulfilment line is not available."),
        "online_orders.invalid_barcode" => new(code, "The barcode does not match this fulfilment line."),
        "online_orders.invalid_quantity" => new(code, "The requested pick quantity is not available."),
        _ => new(code ?? "online_orders.picking_failed", "Online-order picking could not be completed.")
    };

    private static ApplicationResult<PosOnlineOrderPickingResponse> QueryFailure(ApplicationError error) =>
        ApplicationResult<PosOnlineOrderPickingResponse>.Failure(error);

    private static ApplicationResult<PosOnlineOrderPickingCommandResponse> CommandFailure(ApplicationError error) =>
        ApplicationResult<PosOnlineOrderPickingCommandResponse>.Failure(error);

    private static ApplicationResult<PosOnlineOrderPickingNoteCommandResponse> NoteFailure(ApplicationError error) =>
        ApplicationResult<PosOnlineOrderPickingNoteCommandResponse>.Failure(error);
}
