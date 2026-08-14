using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Domain.Modules.Tenant.HardwareCash.Constants;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;

namespace E_POS.Application.Modules.Tenant.HardwareCash.Services;

public sealed class PosDrawerService : IPosDrawerService
{
    private readonly IPosDrawerRepository _repository;
    private readonly ITenantAuthRepository _authRepository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IDateTimeProvider _clock;
    private readonly IPosTillSessionRepository _tillSessions;

    public PosDrawerService(
        IPosDrawerRepository repository,
        ITenantAuthRepository authRepository,
        IPasswordHashService passwordHashService,
        IDateTimeProvider clock,
        IPosTillSessionRepository tillSessions)
    {
        _repository = repository;
        _authRepository = authRepository;
        _passwordHashService = passwordHashService;
        _clock = clock;
        _tillSessions = tillSessions;
    }

    public async Task<ApplicationResult<CashDrawerOperationDto>> RegisterOperationAsync(
        TenantRequestContext context,
        RegisterDrawerOperationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RequestId == Guid.Empty || request.PosDeviceId == Guid.Empty)
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.invalid_request", "Request ID and Device ID are required."));

        var result = await _repository.RegisterOperationAsync(
            context.TenantId,
            context.UserId,
            request,
            null,
            _clock.UtcNow,
            cancellationToken);

        if (result.ErrorCode is not null)
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError(result.ErrorCode, "Failed to register drawer operation."));

        return ApplicationResult<CashDrawerOperationDto>.Success(result.Operation!);
    }

    public async Task<ApplicationResult<CashDrawerOperationDto>> FinalizeOperationAsync(
        TenantRequestContext context,
        Guid operationId,
        FinalizeDrawerOperationRequest request,
        CancellationToken cancellationToken)
    {
        if (operationId == Guid.Empty)
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.invalid_operation_id", "Operation ID is required."));

        var result = await _repository.FinalizeOperationAsync(
            context.TenantId,
            context.UserId,
            operationId,
            request,
            _clock.UtcNow,
            cancellationToken);

        if (result.ErrorCode is not null)
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError(result.ErrorCode, "Failed to finalize drawer operation."));

        return ApplicationResult<CashDrawerOperationDto>.Success(result.Operation!);
    }

    public async Task<ApplicationResult<CashDrawerOperationDto>> ManualOpenDrawerAsync(
        TenantRequestContext context,
        ManualOpenDrawerRequest request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CashDrawerPermissions.Manage))
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.permission_denied", "You do not have permission to manually open the cash drawer."));

        if (request.RequestId == Guid.Empty || request.PosDeviceId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason))
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.invalid_request", "Request ID, Device ID and Reason are required."));

        // Check if manual open is enabled & manager approval is required by active settings
        var settings = await _repository.GetActiveDrawerSettingsAsync(context.TenantId, request.PosDeviceId, cancellationToken);
        if (settings is null)
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.configuration_missing", "Active cash drawer configuration is missing."));

        if (!settings.ManualOpenEnabled)
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.manual_open_disabled", "Manual drawer opening is disabled."));

        Guid? approverId = null;
        var policyRequiresApproval = settings.Policy?.Contains("approval", StringComparison.OrdinalIgnoreCase) == true;

        if (policyRequiresApproval)
        {
            if (string.IsNullOrWhiteSpace(request.ManagerEmail) || string.IsNullOrWhiteSpace(request.ManagerPassword))
            {
                return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.approval_required", "Manager approval is required for manual no-sale drawer open."));
            }

            var normalizedEmail = E_POS.Domain.Modules.Tenant.AccessControl.Entities.TenantUser.NormalizeEmail(request.ManagerEmail);
            var managerAccount = await _authRepository.FindLoginAccountByNormalizedEmailAsync(normalizedEmail, cancellationToken);
            if (managerAccount is null || !string.Equals(managerAccount.UserStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.invalid_approver_credentials", "Invalid manager credentials."));
            }

            if (string.IsNullOrWhiteSpace(managerAccount.PasswordHash) || !_passwordHashService.VerifyPassword(request.ManagerPassword, managerAccount.PasswordHash))
            {
                return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.invalid_approver_credentials", "Invalid manager credentials."));
            }

            var managerPerms = await _authRepository.GetActivePermissionCodesAsync(managerAccount.TenantUserId, context.TenantId, cancellationToken);
            if (!managerPerms.Contains(CashDrawerPermissions.Manage))
            {
                return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.approver_permission_denied", "The approving user does not possess cash drawer management permission."));
            }

            approverId = managerAccount.TenantUserId;
        }

        var registerReq = new RegisterDrawerOperationRequest(
            request.RequestId,
            request.PosDeviceId,
            null,
            "manualNoSale",
            request.Reason);

        var result = await _repository.RegisterOperationAsync(
            context.TenantId,
            context.UserId,
            registerReq,
            approverId,
            _clock.UtcNow,
            cancellationToken);

        if (result.ErrorCode is not null)
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError(result.ErrorCode, "Failed to register manual drawer open operation."));

        return ApplicationResult<CashDrawerOperationDto>.Success(result.Operation!);
    }

    public async Task<ApplicationResult<IReadOnlyList<CashDrawerOperationDto>>> GetHistoryAsync(
        TenantRequestContext context,
        Guid posDeviceId,
        int take,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CashDrawerPermissions.View))
            return ApplicationResult<IReadOnlyList<CashDrawerOperationDto>>.Failure(new ApplicationError("pos_drawer.permission_denied", "You do not have permission to view cash drawer history."));

        if (posDeviceId == Guid.Empty || take is < 1 or > 100)
            return ApplicationResult<IReadOnlyList<CashDrawerOperationDto>>.Failure(new ApplicationError("pos_drawer.invalid_request", "Pos Device ID is required and take must be between 1 and 100."));

        var history = await _repository.GetHistoryAsync(context.TenantId, posDeviceId, take, cancellationToken);
        return ApplicationResult<IReadOnlyList<CashDrawerOperationDto>>.Success(history);
    }

    public async Task<ApplicationResult<CashDrawerOperationDto>> GetOperationStatusAsync(
        TenantRequestContext context,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CashDrawerPermissions.View))
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.permission_denied", "You do not have permission to view cash drawer operations."));

        if (operationId == Guid.Empty)
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.invalid_operation_id", "Operation ID is required."));

        var op = await _repository.GetOperationByIdAsync(context.TenantId, operationId, cancellationToken);
        if (op is null)
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.operation_not_found", "Operation not found."));

        return ApplicationResult<CashDrawerOperationDto>.Success(op);
    }

    public async Task<ApplicationResult<CashDrawerOperationDto>> GetOperationStatusByRequestIdAsync(
        TenantRequestContext context,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CashDrawerPermissions.View))
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.permission_denied", "You do not have permission to view cash drawer operations."));

        if (requestId == Guid.Empty)
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.invalid_request", "Request ID is required."));

        var op = await _repository.GetOperationByRequestIdAsync(context.TenantId, requestId, cancellationToken);
        if (op is null)
            return ApplicationResult<CashDrawerOperationDto>.Failure(new ApplicationError("pos_drawer.operation_not_found", "Operation not found."));

        return ApplicationResult<CashDrawerOperationDto>.Success(op);
    }

    public async Task<ApplicationResult<PosCashDrawerSummaryDto>> GetFinancialSummaryAsync(
        TenantRequestContext context, Guid deviceId, CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CashDrawerPermissions.View))
            return Failure<PosCashDrawerSummaryDto>("cash_drawer.permission_denied", "You do not have permission to view the cash drawer.");
        var session = await ResolveSession(context.TenantId, deviceId, cancellationToken);
        if (!session.IsSuccess || session.Snapshot is null)
            return Failure<PosCashDrawerSummaryDto>(session.ErrorCode!, SessionMessage(session.ErrorCode));
        var summary = await _repository.GetFinancialSummaryAsync(context.TenantId, session.Snapshot.SessionId, cancellationToken);
        return summary is null
            ? Failure<PosCashDrawerSummaryDto>("cash_drawer.till_session_not_open", "No open till session was found for this device.")
            : ApplicationResult<PosCashDrawerSummaryDto>.Success(summary);
    }

    public async Task<ApplicationResult<PosCashDrawerMovementPageDto>> GetFinancialMovementsAsync(
        TenantRequestContext context, Guid deviceId, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CashDrawerPermissions.View))
            return Failure<PosCashDrawerMovementPageDto>("cash_drawer.permission_denied", "You do not have permission to view cash movements.");
        if (page < 1 || pageSize is < 1 or > 100)
            return Failure<PosCashDrawerMovementPageDto>("cash_drawer.invalid_pagination", "Page must be positive and pageSize must be between 1 and 100.");
        var session = await ResolveSession(context.TenantId, deviceId, cancellationToken);
        if (!session.IsSuccess || session.Snapshot is null)
            return Failure<PosCashDrawerMovementPageDto>(session.ErrorCode!, SessionMessage(session.ErrorCode));
        return ApplicationResult<PosCashDrawerMovementPageDto>.Success(
            await _repository.GetFinancialMovementsAsync(context.TenantId, session.Snapshot.SessionId, page, pageSize, cancellationToken));
    }

    public async Task<ApplicationResult<PosCashDrawerMovementDto>> CreateFinancialMovementAsync(
        TenantRequestContext context, CreatePosCashMovementRequest request, CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CashDrawerPermissions.CreateMovement))
            return Failure<PosCashDrawerMovementDto>("cash_drawer.permission_denied", "You do not have permission to create cash movements.");
        if (request.RequestId == Guid.Empty || request.DeviceId == Guid.Empty || request.TillSessionId == Guid.Empty)
            return Failure<PosCashDrawerMovementDto>("cash_drawer.invalid_request", "Request, device and till session ids are required.");
        var type = request.MovementType.Trim().ToUpperInvariant();
        if (type is not ("CASH_IN" or "CASH_OUT" or "CASH_DROP"))
            return Failure<PosCashDrawerMovementDto>("cash_drawer.invalid_movement_type", "Movement type is not supported.");
        if (request.Amount <= 0)
            return Failure<PosCashDrawerMovementDto>("cash_drawer.invalid_amount", "Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 250)
            return Failure<PosCashDrawerMovementDto>("cash_drawer.invalid_reason", "A reason of at most 250 characters is required.");
        if (request.ReferenceNumber?.Trim().Length > 100)
            return Failure<PosCashDrawerMovementDto>("cash_drawer.invalid_reference", "Reference number must not exceed 100 characters.");

        var session = await ResolveSession(context.TenantId, request.DeviceId, cancellationToken);
        if (!session.IsSuccess || session.Snapshot is null)
            return Failure<PosCashDrawerMovementDto>(session.ErrorCode!, SessionMessage(session.ErrorCode));
        if (session.Snapshot.SessionId != request.TillSessionId)
            return Failure<PosCashDrawerMovementDto>("cash_drawer.till_session_mismatch", "The requested till session does not match this device.");

        var result = await _repository.CreateFinancialMovementAsync(
            context.TenantId, context.UserId, session.Snapshot.TillId, request, _clock.UtcNow, cancellationToken);
        return result.ErrorCode is null && result.Movement is not null
            ? ApplicationResult<PosCashDrawerMovementDto>.Success(result.Movement)
            : Failure<PosCashDrawerMovementDto>(result.ErrorCode!, SessionMessage(result.ErrorCode));
    }

    private Task<CurrentTillSessionResolveResult> ResolveSession(Guid tenantId, Guid deviceId, CancellationToken cancellationToken) =>
        deviceId == Guid.Empty
            ? Task.FromResult(new CurrentTillSessionResolveResult(false, "cash_drawer.invalid_device_id", null))
            : _tillSessions.ResolveCurrentSessionAsync(tenantId, deviceId, cancellationToken);

    private static ApplicationResult<T> Failure<T>(string code, string message) =>
        ApplicationResult<T>.Failure(new ApplicationError(code, message));

    private static string SessionMessage(string? code) => code switch
    {
        "till_session.device_not_found" => "POS device could not be found.",
        "till_session.device_not_trusted" => "This POS device is not trusted.",
        "till_session.till_not_assigned" => "No till is assigned to this POS device.",
        "till_session.not_found" or "cash_drawer.till_session_not_open" => "No open till session was found for this device.",
        "cash_drawer.idempotency_conflict" => "The request id was already used for a different movement.",
        "cash_drawer.insufficient_expected_cash" => "The movement amount exceeds current expected cash.",
        _ => "Cash drawer request could not be completed."
    };
}
